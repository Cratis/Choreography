// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// Tracks live collaborator connections and broadcasts presence over two-way WebSockets. Presence is
/// ephemeral - a disconnect IS a leave - so nothing is persisted; the socket lifetime is the source of truth.
/// Each replica fans events out to its own connected sockets directly (the fast path) and defers the cross-replica
/// part to an injected <see cref="ICollaborationBackplane"/>, so the same code is correct at one replica (the
/// backplane is a no-op) and at many (the backplane shares the roster and relays events between replicas).
/// </summary>
/// <remarks>
/// Register the hub as a singleton; <c language="csharp">AddCratisCollaboration</c> does so.
/// </remarks>
public class CollaborationHub : ICollaborationHub, ILocalCollaborationDelivery
{
    static readonly TimeSpan _idleAfter = TimeSpan.FromSeconds(30);
    static readonly TimeSpan _pingInterval = TimeSpan.FromSeconds(20);

    /// <summary>
    /// The client answers every ping with a pong (and sends presence), so a connection that has said nothing for
    /// this long is half-open (sleeping laptop, dropped network, dead NAT mapping): treat it as left instead of
    /// waiting for TCP to notice, which can take many minutes and shows as a ghost collaborator meanwhile.
    /// </summary>
    static readonly TimeSpan _silentAfter = TimeSpan.FromSeconds(75);
    static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    readonly ConcurrentDictionary<string, ConcurrentDictionary<string, Connection>> _groups = new();
    readonly ICollaborationBackplane _backplane;

    /// <summary>
    /// Initializes a new instance of the <see cref="CollaborationHub"/> class.
    /// </summary>
    /// <param name="backplane">The cross-replica backplane (a no-op at a single replica).</param>
    public CollaborationHub(ICollaborationBackplane backplane)
    {
        _backplane = backplane;
        _backplane.Attach(this);
    }

    /// <inheritdoc/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "A connection removed from its group is disposed by the Run call that owns it, once its own socket loop has ended.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2025:Do not pass disposable objects to tasks that may outlive them", Justification = "The ping loop is awaited in the finally block before the connection is disposed.")]
    public async Task Run(string groupKey, string sessionId, string userId, string displayName, string color, WebSocket socket, CancellationToken cancellationToken, bool hasAvatar = false)
    {
        var group = _groups.GetOrAdd(groupKey, _ => new());
        var connectionId = Guid.NewGuid();
        var connection = new Connection(socket, new Collaborator(sessionId, userId, displayName, color, 0, 0, string.Empty, string.Empty, IsIdle: false, HasAvatar: hasAvatar));

        // A reconnect of the same session on this replica replaces the old (dead) socket.
        if (group.TryRemove(sessionId, out var stale))
        {
            await stale.CloseSilently();
        }

        group[sessionId] = connection;

        var pinger = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var pingLoop = Task.CompletedTask;
        try
        {
            var joined = await _backplane.Join(groupKey, connection.State, connectionId);
            if (!joined.TryGetResult(out var remoteOthers))
            {
                await connection.Reconnect();
                return;
            }

            await BroadcastLocal(group, exceptSessionId: sessionId, new { type = "joined", collaborator = connection.State });
            var localOthers = group.Values.Where(c => c.State.SessionId != sessionId).Select(c => c.State);
            var others = localOthers.Concat(remoteOthers).GroupBy(c => c.SessionId).Select(g => g.Last());
            await Send(connection, new { type = "roster", userId, collaborators = others });
            connection.Joined = true;
            pingLoop = PingLoop(groupKey, group, connection, pinger.Token);
            await ReceiveLoop(groupKey, group, connection, cancellationToken);
        }
        finally
        {
            connection.Joined = false;
            await pinger.CancelAsync();
            await pingLoop;
            pinger.Dispose();

            // Only the connection that still owns the session announces the leave - a replaced socket
            // (same session reconnected here) must not remove its successor.
            if (group.TryGetValue(sessionId, out var current) && ReferenceEquals(current, connection))
            {
                group.TryRemove(sessionId, out _);
                await BroadcastLocal(group, exceptSessionId: sessionId, new { type = "left", sessionId });
                await _backplane.Leave(groupKey, sessionId, connectionId);
            }

            if (group.IsEmpty)
            {
                _groups.TryRemove(groupKey, out _);
                await _backplane.Release(groupKey);
            }

            await connection.CloseSilently();
            connection.Dispose();
        }
    }

    /// <inheritdoc/>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "A connection removed from its group is disposed by the Run call that owns it, once its own socket loop has ended.")]
    public async Task DeliverJoined(string groupKey, Collaborator collaborator)
    {
        // The session joined on another replica, so any connection this replica still holds for it is a leftover of a
        // reconnect (typically half-open). Drop it quietly: its eventual leave must neither announce the session as
        // gone to our sockets nor touch the roster entry that now belongs to the newer connection.
        if (_groups.TryGetValue(groupKey, out var group) && group.TryRemove(collaborator.SessionId, out var superseded))
        {
            await superseded.Reconnect();
        }

        await BroadcastToGroup(groupKey, collaborator.SessionId, new { type = "joined", collaborator });
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> JoinedSessions(string groupKey) =>
        _groups.TryGetValue(groupKey, out var group)
            ? [.. group.Values.Where(connection => connection.Joined).Select(connection => connection.State.SessionId)]
            : [];

    /// <inheritdoc/>
    public async Task DeliverRejoin(string groupKey, string sessionId)
    {
        if (_groups.TryGetValue(groupKey, out var group) && group.TryGetValue(sessionId, out var connection))
        {
            await connection.Reconnect();
        }
    }

    /// <inheritdoc/>
    public Task DeliverLeft(string groupKey, string sessionId) =>
        BroadcastToGroup(groupKey, sessionId, new { type = "left", sessionId });

    /// <inheritdoc/>
    public Task DeliverPresence(string groupKey, string sessionId, double x, double y, string area, string surfaceId) =>
        BroadcastToGroup(groupKey, sessionId, new { type = "presence", sessionId, x, y, area, surfaceId });

    /// <inheritdoc/>
    public Task DeliverIdle(string groupKey, string sessionId, bool isIdle) =>
        BroadcastToGroup(groupKey, sessionId, new { type = "idle", sessionId, isIdle });

    /// <inheritdoc/>
    public async Task DeliverReconnect(string groupKey)
    {
        if (_groups.TryGetValue(groupKey, out var group))
        {
            foreach (var connection in group.Values)
            {
                await connection.Reconnect();
            }
        }
    }

    static Task Send(Connection connection, object message) =>
        connection.SendRaw(JsonSerializer.SerializeToUtf8Bytes(message, _json));

    async Task ReceiveLoop(string groupKey, ConcurrentDictionary<string, Connection> group, Connection connection, CancellationToken cancellationToken)
    {
        var buffer = new byte[4 * 1024];
        while (connection.Socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            WebSocketReceiveResult received;
            try
            {
                received = await connection.Socket.ReceiveAsync(buffer, cancellationToken);
            }
            catch (WebSocketException)
            {
                return; // client vanished - the finally block broadcasts the leave
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (received.MessageType == WebSocketMessageType.Close)
            {
                return;
            }

            connection.LastReceived = DateTimeOffset.UtcNow;
            PresenceMessage? message;
            try
            {
                message = JsonSerializer.Deserialize<PresenceMessage>(buffer.AsSpan(0, received.Count), _json);
            }
            catch (JsonException)
            {
                continue; // malformed frame - ignore rather than dropping the session
            }

            if (message?.Type != "presence")
            {
                continue; // pongs and unknown types need no handling
            }

            var moved = connection.State.X != message.X || connection.State.Y != message.Y;
            connection.State = connection.State with
            {
                X = message.X,
                Y = message.Y,
                Area = message.Area ?? string.Empty,
                SurfaceId = message.SurfaceId ?? string.Empty,
                IsIdle = !moved && connection.State.IsIdle,
            };
            if (moved)
            {
                connection.LastActive = DateTimeOffset.UtcNow;
            }

            await BroadcastLocal(group, exceptSessionId: connection.State.SessionId, new
            {
                type = "presence",
                sessionId = connection.State.SessionId,
                x = message.X,
                y = message.Y,
                area = connection.State.Area,
                surfaceId = connection.State.SurfaceId,
            });
            if (!await _backplane.Presence(groupKey, connection.State.SessionId, message.X, message.Y, connection.State.Area, connection.State.SurfaceId))
            {
                await connection.Reconnect();
                return;
            }
        }
    }

    /// <summary>
    /// Periodic keep-alive ping doubling as the idle sweep: marks a connection idle once its cursor has been
    /// still beyond the threshold and broadcasts the transition. A failed ping send drops out of the loop; the
    /// receive loop then observes the dead socket and performs the leave.
    /// </summary>
    /// <param name="groupKey">The group key the connection belongs to.</param>
    /// <param name="group">The group the connection belongs to.</param>
    /// <param name="connection">The connection to ping and idle-sweep.</param>
    /// <param name="cancellationToken">Token cancelled when the session ends.</param>
    /// <returns>A <see cref="Task"/> completing when the session ends.</returns>
    async Task PingLoop(string groupKey, ConcurrentDictionary<string, Connection> group, Connection connection, CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(_pingInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (DateTimeOffset.UtcNow - connection.LastReceived > _silentAfter)
                {
                    await connection.Reconnect();
                    return;
                }

                await Send(connection, new { type = "ping" });

                var shouldBeIdle = DateTimeOffset.UtcNow - connection.LastActive > _idleAfter;
                if (shouldBeIdle != connection.State.IsIdle)
                {
                    connection.State = connection.State with { IsIdle = shouldBeIdle };
                    await BroadcastLocal(group, exceptSessionId: connection.State.SessionId, new { type = "idle", sessionId = connection.State.SessionId, isIdle = shouldBeIdle });
                    if (!await _backplane.Idle(groupKey, connection.State.SessionId, shouldBeIdle))
                    {
                        await connection.Reconnect();
                        return;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // session ended - nothing to do
        }
    }

    async Task BroadcastLocal(ConcurrentDictionary<string, Connection> group, string exceptSessionId, object message)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(message, _json);
        foreach (var member in group.Values)
        {
            if (member.State.SessionId == exceptSessionId)
            {
                continue;
            }

            await member.SendRaw(payload);
        }
    }

    Task BroadcastToGroup(string groupKey, string exceptSessionId, object message) =>
        _groups.TryGetValue(groupKey, out var group) ? BroadcastLocal(group, exceptSessionId, message) : Task.CompletedTask;

    sealed record PresenceMessage(string Type, double X, double Y, string? Area, string? SurfaceId);

    sealed class Connection(WebSocket socket, Collaborator state) : IDisposable
    {
        readonly SemaphoreSlim _sendLock = new(1, 1);

        public WebSocket Socket => socket;

        public Collaborator State { get; set; } = state;

        public DateTimeOffset LastActive { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset LastReceived { get; set; } = DateTimeOffset.UtcNow;

        public bool Joined { get; set; }

        public void Dispose() => _sendLock.Dispose();

        public async Task SendRaw(byte[] payload)
        {
            if (Socket.State != WebSocketState.Open)
            {
                return;
            }

            await _sendLock.WaitAsync();
            try
            {
                await Socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
            }
            catch (WebSocketException)
            {
                // dead socket - the receive loop notices and leaves
            }
            catch (ObjectDisposedException)
            {
                // socket torn down concurrently with a broadcast - same outcome as a dead socket
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async Task Reconnect()
        {
            try
            {
                if (Socket.State == WebSocketState.Open)
                {
                    await Socket.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable, "Collaboration is reconnecting", CancellationToken.None);
                }
            }
            catch (WebSocketException)
            {
                // The client already disconnected and will use its normal reconnect path.
            }
            finally
            {
                Socket.Abort();
            }
        }

        public async Task CloseSilently()
        {
            try
            {
                if (Socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                {
                    await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
                }
            }
            catch (WebSocketException)
            {
                // already gone
            }
            catch (ObjectDisposedException)
            {
                // already gone
            }
        }
    }
}
