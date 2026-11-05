// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// The pure, in-memory bookkeeping for one room's collaboration roster: who is present across every replica,
/// where their cursor is, and which connection owns each session. It holds no Orleans concerns so it can be
/// unit-tested directly; <see cref="CollaborationRoomGrain"/> wraps it with the observer fan-out.
/// </summary>
/// <param name="timeProvider">The clock used to judge how long a session has gone without a sign of life; the system clock when omitted.</param>
public sealed class CollaborationRoster(TimeProvider? timeProvider = null)
{
    readonly Dictionary<string, RosterEntry> _entries = [];
    readonly HashSet<Guid> _refreshingPods = [];
    readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Gets a value indicating whether the roster has no members.
    /// </summary>
    public bool IsEmpty => _entries.Count == 0;

    /// <summary>
    /// Gets every collaborator currently in the roster.
    /// </summary>
    public IReadOnlyList<Collaborator> All => [.. _entries.Values.Select(entry => entry.Collaborator)];

    /// <summary>
    /// Adds (or replaces, on reconnect) a session and returns the collaborators present on OTHER replicas - the
    /// ones the joining replica does not already know from its own local sockets.
    /// </summary>
    /// <param name="collaborator">The joining collaborator.</param>
    /// <param name="connectionId">The per-connection token used to make a later cross-replica leave race-safe.</param>
    /// <param name="originPodId">The replica the join came from.</param>
    /// <returns>The collaborators on replicas other than <paramref name="originPodId"/>.</returns>
    public IReadOnlyList<Collaborator> Join(Collaborator collaborator, Guid connectionId, Guid originPodId)
    {
        _entries[collaborator.SessionId] = new RosterEntry(collaborator, connectionId, originPodId, _timeProvider.GetUtcNow());
        return [.. _entries.Values.Where(entry => entry.OriginPodId != originPodId).Select(entry => entry.Collaborator)];
    }

    /// <summary>
    /// Removes a session, unless a newer connection for the same session has since joined (a cross-replica
    /// reconnect), identified by <paramref name="connectionId"/>.
    /// </summary>
    /// <param name="sessionId">The session to remove.</param>
    /// <param name="connectionId">The token from the corresponding <see cref="Join"/>.</param>
    /// <returns><see langword="true"/> if the session was removed; otherwise <see langword="false"/>.</returns>
    public bool Leave(string sessionId, Guid connectionId)
    {
        if (!_entries.TryGetValue(sessionId, out var entry) || entry.ConnectionId != connectionId)
        {
            return false;
        }

        _entries.Remove(sessionId);
        return true;
    }

    /// <summary>
    /// Updates a session's cursor and area.
    /// </summary>
    /// <param name="sessionId">The session that moved.</param>
    /// <param name="x">The cursor X position.</param>
    /// <param name="y">The cursor Y position.</param>
    /// <param name="area">The area the collaborator is in.</param>
    /// <param name="surfaceId">The surface the collaborator is looking at.</param>
    public void Presence(string sessionId, double x, double y, string area, string surfaceId)
    {
        if (_entries.TryGetValue(sessionId, out var entry))
        {
            // Moving the cursor is activity: mirror the hub, which clears idle on movement without announcing it.
            var moved = entry.Collaborator.X != x || entry.Collaborator.Y != y;
            _entries[sessionId] = entry with { Collaborator = entry.Collaborator with { X = x, Y = y, Area = area, SurfaceId = surfaceId, IsIdle = !moved && entry.Collaborator.IsIdle }, LastSeen = _timeProvider.GetUtcNow() };
        }
    }

    /// <summary>
    /// Updates a session's idle state.
    /// </summary>
    /// <param name="sessionId">The session whose idle state changed.</param>
    /// <param name="isIdle">Whether the collaborator is now idle.</param>
    public void Idle(string sessionId, bool isIdle)
    {
        if (_entries.TryGetValue(sessionId, out var entry))
        {
            _entries[sessionId] = entry with { Collaborator = entry.Collaborator with { IsIdle = isIdle }, LastSeen = _timeProvider.GetUtcNow() };
        }
    }

    /// <summary>
    /// Records that a replica is alive and which of its sessions it still holds an open connection for. A replica
    /// that refreshes is from then on held to its word: a session of that replica that is not refreshed within the
    /// expiry is dropped by <see cref="Expire"/>.
    /// </summary>
    /// <param name="originPodId">The replica that is refreshing.</param>
    /// <param name="sessionIds">The sessions the replica still has an open connection for.</param>
    /// <returns>The sessions in <paramref name="sessionIds"/> this roster does not know (it lost them), so the replica can re-establish them.</returns>
    public IReadOnlyList<string> Refresh(Guid originPodId, IReadOnlyCollection<string> sessionIds)
    {
        _refreshingPods.Add(originPodId);
        var now = _timeProvider.GetUtcNow();
        var unknown = new List<string>();
        foreach (var sessionId in sessionIds)
        {
            if (_entries.TryGetValue(sessionId, out var entry) && entry.OriginPodId == originPodId)
            {
                _entries[sessionId] = entry with { LastSeen = now };
            }
            else
            {
                unknown.Add(sessionId);
            }
        }

        return unknown;
    }

    /// <summary>
    /// Removes the sessions whose replica has stopped vouching for them: replicas that were killed or rolled out
    /// (out of memory, deployment) never send a leave, and an unreachable socket never closes. Only replicas that
    /// have ever refreshed are expired, so a replica running a version that does not refresh keeps its sessions
    /// until it leaves them itself.
    /// </summary>
    /// <param name="timeToLive">How long a session may go without a sign of life from its replica.</param>
    /// <returns>The removed sessions.</returns>
    public IReadOnlyList<Collaborator> Expire(TimeSpan timeToLive)
    {
        var threshold = _timeProvider.GetUtcNow() - timeToLive;
        var expired = _entries.Values
            .Where(entry => _refreshingPods.Contains(entry.OriginPodId) && entry.LastSeen < threshold)
            .Select(entry => entry.Collaborator)
            .ToList();
        foreach (var collaborator in expired)
        {
            _entries.Remove(collaborator.SessionId);
        }

        return expired;
    }

    sealed record RosterEntry(Collaborator Collaborator, Guid ConnectionId, Guid OriginPodId, DateTimeOffset LastSeen);
}
