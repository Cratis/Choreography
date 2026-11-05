// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Orleans.Utilities;

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// Holds one room's authoritative collaboration roster and fans presence changes out to every replica
/// subscribed for that room. See <see cref="ICollaborationRoomGrain"/>.
/// </summary>
/// <param name="logger">The logger used by the observer manager.</param>
[GrainType("cratis-choreography-collaboration-room")]
public class CollaborationRoomGrain(ILogger<CollaborationRoomGrain> logger) : Grain, ICollaborationRoomGrain
{
    /// <summary>
    /// The replicas vouch for their sessions every 15 s (see the backplane); a session nobody vouched for within
    /// this window belongs to a replica that is gone.
    /// </summary>
    internal static readonly TimeSpan SessionTimeToLive = TimeSpan.FromSeconds(60);

    static readonly TimeSpan _expirySweepInterval = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Observers are re-subscribed by each replica on a heartbeat well inside this window; a replica that dies stops
    /// heartbeating and is pruned, so its sessions are also cleaned up (see the reaper in the backplane).
    /// </summary>
    static readonly TimeSpan _observerExpiration = TimeSpan.FromMinutes(2);

    readonly ObserverManager<ICollaborationRoomObserver> _observers = new(_observerExpiration, logger);
    readonly CollaborationRoster _roster = new();

    string GroupKey => this.GetPrimaryKeyString();

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        this.RegisterGrainTimer(ExpireStaleSessions, _expirySweepInterval, _expirySweepInterval);
        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task Subscribe(ICollaborationRoomObserver observer)
    {
        _observers.Subscribe(observer, observer);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Unsubscribe(ICollaborationRoomObserver observer)
    {
        _observers.Unsubscribe(observer);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Collaborator>> Join(Collaborator collaborator, Guid connectionId, Guid originPodId)
    {
        ExpireSessions();
        var othersOnOtherPods = _roster.Join(collaborator, connectionId, originPodId);
        _observers.Notify(observer => observer.OnJoined(GroupKey, collaborator, originPodId));
        return Task.FromResult(othersOnOtherPods);
    }

    /// <inheritdoc/>
    public Task Leave(string sessionId, Guid connectionId, Guid originPodId)
    {
        // A stale leave (the same session reconnected onto another replica) must not evict the newer connection.
        if (!_roster.Leave(sessionId, connectionId))
        {
            return Task.CompletedTask;
        }

        _observers.Notify(observer => observer.OnLeft(GroupKey, sessionId, originPodId));

        if (_roster.IsEmpty)
        {
            DeactivateOnIdle();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<string>> Refresh(Guid originPodId, IReadOnlyList<string> sessionIds)
    {
        var unknown = _roster.Refresh(originPodId, sessionIds);
        ExpireSessions();
        return Task.FromResult(unknown);
    }

    /// <inheritdoc/>
    public Task Presence(string sessionId, double x, double y, string area, string surfaceId, Guid originPodId)
    {
        _roster.Presence(sessionId, x, y, area, surfaceId);
        _observers.Notify(observer => observer.OnPresence(GroupKey, sessionId, x, y, area, surfaceId, originPodId));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Idle(string sessionId, bool isIdle, Guid originPodId)
    {
        _roster.Idle(sessionId, isIdle);
        _observers.Notify(observer => observer.OnIdle(GroupKey, sessionId, isIdle, originPodId));
        return Task.CompletedTask;
    }

    Task ExpireStaleSessions()
    {
        ExpireSessions();
        return Task.CompletedTask;
    }

    void ExpireSessions()
    {
        var expired = _roster.Expire(SessionTimeToLive);
        foreach (var collaborator in expired)
        {
            // Guid.Empty: no replica is the origin of an expiry, so every replica (including the one the session was
            // thought to live on) tells its sockets the session is gone.
            _observers.Notify(observer => observer.OnLeft(GroupKey, collaborator.SessionId, Guid.Empty));
        }

        if (expired.Count > 0 && _roster.IsEmpty)
        {
            DeactivateOnIdle();
        }
    }
}
