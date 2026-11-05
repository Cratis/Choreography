// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Monads;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// The multi-replica backplane: it keeps each room's roster in an <see cref="ICollaborationRoomGrain"/>
/// and receives the other replicas' presence events through a grain observer, handing them to the local hub. It is
/// registered only by <c language="csharp">AddCratisCollaborationOrleans</c>, when the application runs in an Orleans cluster with more than
/// one replica. As an <see cref="IHostedService"/> it re-subscribes its observer periodically so the grains keep it alive.
/// </summary>
public sealed partial class OrleansCollaborationBackplane : BackgroundService, ICollaborationBackplane
{
    /// <summary>
    /// Re-subscribe well inside the grain's observer expiration so a live replica is never pruned: every third vouch.
    /// </summary>
    const int ResubscribeEveryNthVouch = 3;

    /// <summary>
    /// Vouch for this replica's sessions well inside the grain's session time to live (see CollaborationRoomGrain).
    /// </summary>
    static readonly TimeSpan _vouchInterval = TimeSpan.FromSeconds(15);

    readonly IGrainFactory _grainFactory;
    readonly ILogger<OrleansCollaborationBackplane> _logger;
    readonly ConcurrentDictionary<string, byte> _subscribedGroups = new();
    readonly ConcurrentDictionary<string, byte> _recoveringGroups = new();
    readonly CollaborationRetry _retry;
    readonly Guid _podId = Guid.NewGuid();

    /// <summary>
    /// Orleans holds the object passed to CreateObjectReference() only via a WeakReference — the returned
    /// reference does NOT keep it alive. Without a durable strong reference here, the .NET GC can reclaim
    /// _observer at any time; the first message dispatched to it afterward finds a dead target and
    /// deregisters it permanently on this replica, and every subsequent cross-replica push to this replica then fails
    /// with "Unexpected target grain in request" for the rest of the process's life (invisibly — one-way
    /// IGrainObserver calls have no failure callback). Keeping _observer alongside _observerReference for the
    /// singleton's lifetime is what prevents that.
    /// </summary>
    readonly Observer _observer;
    readonly ICollaborationRoomObserver _observerReference;

    ILocalCollaborationDelivery? _delivery;

    /// <summary>
    /// Initializes a new instance of the <see cref="OrleansCollaborationBackplane"/> class.
    /// </summary>
    /// <param name="grainFactory">The Orleans grain factory.</param>
    /// <param name="logger">The logger.</param>
    public OrleansCollaborationBackplane(IGrainFactory grainFactory, ILogger<OrleansCollaborationBackplane> logger)
    {
        _grainFactory = grainFactory;
        _logger = logger;
        _retry = new CollaborationRetry();
        _observer = new Observer(this);
        _observerReference = _grainFactory.CreateObjectReference<ICollaborationRoomObserver>(_observer);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OrleansCollaborationBackplane"/> class with a specific retry policy.
    /// </summary>
    /// <param name="grainFactory">The Orleans grain factory.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="retry">The retry policy for cluster operations.</param>
    internal OrleansCollaborationBackplane(IGrainFactory grainFactory, ILogger<OrleansCollaborationBackplane> logger, CollaborationRetry retry)
        : this(grainFactory, logger) => _retry = retry;

    /// <inheritdoc/>
    public void Attach(ILocalCollaborationDelivery delivery) => _delivery = delivery;

    /// <inheritdoc/>
    public Task<Result<IReadOnlyList<Collaborator>, CollaborationUnavailable>> Join(string groupKey, Collaborator collaborator, Guid connectionId) =>
        _retry.Execute(
            async () =>
            {
                var grain = _grainFactory.GetGrain<ICollaborationRoomGrain>(groupKey);
                await EnsureSubscribed(groupKey, grain, refresh: true);
                return await grain.Join(collaborator, connectionId, _podId);
            },
            () => Invalidate(groupKey));

    /// <inheritdoc/>
    public Task<bool> Leave(string groupKey, string sessionId, Guid connectionId) =>
        Execute(groupKey, grain => grain.Leave(sessionId, connectionId, _podId));

    /// <inheritdoc/>
    public Task<bool> Presence(string groupKey, string sessionId, double x, double y, string area, string surfaceId) =>
        Execute(groupKey, grain => grain.Presence(sessionId, x, y, area, surfaceId, _podId));

    /// <inheritdoc/>
    public Task<bool> Idle(string groupKey, string sessionId, bool isIdle) =>
        Execute(groupKey, grain => grain.Idle(sessionId, isIdle, _podId));

    /// <inheritdoc/>
    public async Task Release(string groupKey)
    {
        var wasRecovering = _recoveringGroups.TryRemove(groupKey, out _);
        if (_subscribedGroups.TryRemove(groupKey, out _) || wasRecovering)
        {
            try
            {
                await _retry.Execute(
                    async () =>
                    {
                        await _grainFactory.GetGrain<ICollaborationRoomGrain>(groupKey).Unsubscribe(_observerReference);
                        return true;
                    },
                    () => _subscribedGroups.TryRemove(groupKey, out _));
            }
            catch (Exception exception)
            {
                // Best-effort cleanup — the grain may already have deactivated once its roster emptied.
                LogUnsubscribeFailed(_logger, exception, groupKey);
            }
        }
    }

    /// <summary>
    /// Tells the room's grain which sessions this replica still holds a connection for. Sessions of a replica that dies
    /// stop being vouched for and expire in the grain; sessions the grain lost are re-established by their
    /// sockets reconnecting. Deliberately outside the retry/invalidate path: an older replica still running the
    /// previous grain version cannot vouch, and that must never be mistaken for a lost cluster.
    /// </summary>
    /// <param name="groupKey">The group to vouch for.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal async Task Vouch(string groupKey)
    {
        var delivery = _delivery;
        if (delivery is null || !_subscribedGroups.ContainsKey(groupKey))
        {
            return;
        }

        try
        {
            var sessions = delivery.JoinedSessions(groupKey);
            var grain = _grainFactory.GetGrain<ICollaborationRoomGrain>(groupKey);
            var unknown = await grain.Refresh(_podId, sessions).WaitAsync(TimeSpan.FromSeconds(10));
            foreach (var sessionId in unknown)
            {
                await delivery.DeliverRejoin(groupKey, sessionId);
            }
        }
        catch (Exception exception)
        {
            LogRefreshFailed(_logger, exception, groupKey);
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_vouchInterval);
        var ticks = 0;
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            ticks++;
            foreach (var groupKey in _subscribedGroups.Keys.Concat(_recoveringGroups.Keys).Distinct())
            {
                if (ticks % ResubscribeEveryNthVouch == 0 || _recoveringGroups.ContainsKey(groupKey))
                {
                    try
                    {
                        await Execute(groupKey, _ => Task.CompletedTask, refresh: true);
                    }
                    catch (Exception exception)
                    {
                        // Best-effort keep-alive — a transient failure is recovered on the next tick.
                        LogResubscribeFailed(_logger, exception, groupKey);
                    }
                }

                await Vouch(groupKey);
            }
        }
    }

    void Invalidate(string groupKey)
    {
        if (_subscribedGroups.TryRemove(groupKey, out _))
        {
            _recoveringGroups[groupKey] = 0;
        }
    }

    async Task<bool> Execute(string groupKey, Func<ICollaborationRoomGrain, Task> operation, bool refresh = false)
    {
        var result = await _retry.Execute(
            async () =>
            {
                var grain = _grainFactory.GetGrain<ICollaborationRoomGrain>(groupKey);
                await EnsureSubscribed(groupKey, grain, refresh);
                await operation(grain);
                return true;
            },
            () => Invalidate(groupKey));
        return result.TryGetResult(out _);
    }

    async Task EnsureSubscribed(string groupKey, ICollaborationRoomGrain grain, bool refresh)
    {
        if (!refresh && _subscribedGroups.ContainsKey(groupKey))
        {
            return;
        }

        await grain.Subscribe(_observerReference);

        // Never cache an in-flight or failed subscription. A replacement activation can also lose its
        // ephemeral roster: ask existing sockets to rejoin instead of reviving stale connection tokens.
        _subscribedGroups[groupKey] = 0;
        if (_recoveringGroups.TryRemove(groupKey, out _) && _delivery is not null)
        {
            await _delivery.DeliverReconnect(groupKey);
        }
    }

    void Dispatch(Guid originPodId, Func<ILocalCollaborationDelivery, Task> deliver)
    {
        // Ignore this replica's own echoes — the hub already delivered them to its local sockets directly.
        if (originPodId == _podId)
        {
            return;
        }

        var delivery = _delivery;
        if (delivery is null)
        {
            return;
        }

        _ = DeliverSafely(deliver, delivery);
    }

    async Task DeliverSafely(Func<ILocalCollaborationDelivery, Task> deliver, ILocalCollaborationDelivery delivery)
    {
        try
        {
            await deliver(delivery);
        }
        catch (Exception exception)
        {
            // A dead local socket is expected — the receive loop tears it down and broadcasts the leave.
            LogDeliveryFailed(_logger, exception);
        }
    }

    /// <summary>
    /// The grain-observer object registered with each room grain. It routes remote presence events to the
    /// replica's hub, skipping this replica's own echoes.
    /// </summary>
    /// <param name="backplane">The owning backplane.</param>
    sealed class Observer(OrleansCollaborationBackplane backplane) : ICollaborationRoomObserver
    {
        public void OnJoined(string groupKey, Collaborator collaborator, Guid originPodId) =>
            backplane.Dispatch(originPodId, delivery => delivery.DeliverJoined(groupKey, collaborator));

        public void OnLeft(string groupKey, string sessionId, Guid originPodId) =>
            backplane.Dispatch(originPodId, delivery => delivery.DeliverLeft(groupKey, sessionId));

        public void OnPresence(string groupKey, string sessionId, double x, double y, string area, string surfaceId, Guid originPodId) =>
            backplane.Dispatch(originPodId, delivery => delivery.DeliverPresence(groupKey, sessionId, x, y, area, surfaceId));

        public void OnIdle(string groupKey, string sessionId, bool isIdle, Guid originPodId) =>
            backplane.Dispatch(originPodId, delivery => delivery.DeliverIdle(groupKey, sessionId, isIdle));
    }
}
