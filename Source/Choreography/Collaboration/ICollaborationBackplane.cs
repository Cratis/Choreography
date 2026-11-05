// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// Propagates room presence across the replicas of an application. The <see cref="CollaborationHub"/> always
/// delivers events to its own local sockets directly (the fast path); the backplane is responsible only for the
/// cross-replica part - sharing the roster and relaying events to and from the other replicas. The single-replica
/// implementation is a no-op, so with one replica the hub behaves as a pure in-memory instance.
/// </summary>
public interface ICollaborationBackplane
{
    /// <summary>
    /// Attaches the hub that remote events are delivered to. Called once at startup.
    /// </summary>
    /// <param name="delivery">The local delivery channel.</param>
    void Attach(ILocalCollaborationDelivery delivery);

    /// <summary>
    /// Registers a joining session in the shared roster and relays the join to the other replicas.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <param name="collaborator">The joining collaborator.</param>
    /// <param name="connectionId">A per-connection token making a cross-replica reconnect race-safe.</param>
    /// <returns>The collaborators present on OTHER replicas, to merge into the joining client's snapshot.</returns>
    Task<Result<IReadOnlyList<Collaborator>, CollaborationUnavailable>> Join(string groupKey, Collaborator collaborator, Guid connectionId);

    /// <summary>
    /// Removes a session from the shared roster and relays the leave to the other replicas.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <param name="sessionId">The session leaving.</param>
    /// <param name="connectionId">The token from the corresponding <see cref="Join"/>.</param>
    /// <returns><see langword="true"/> when the cluster handled the leave; <see langword="false"/> when it stayed unavailable.</returns>
    Task<bool> Leave(string groupKey, string sessionId, Guid connectionId);

    /// <summary>
    /// Relays a cursor and area update to the other replicas.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <param name="sessionId">The session that moved.</param>
    /// <param name="x">The cursor X position.</param>
    /// <param name="y">The cursor Y position.</param>
    /// <param name="area">The area the collaborator is in.</param>
    /// <param name="surfaceId">The surface the collaborator is looking at.</param>
    /// <returns><see langword="true"/> when the cluster handled the update; <see langword="false"/> when it stayed unavailable.</returns>
    Task<bool> Presence(string groupKey, string sessionId, double x, double y, string area, string surfaceId);

    /// <summary>
    /// Relays an idle-state change to the other replicas.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <param name="sessionId">The session whose idle state changed.</param>
    /// <param name="isIdle">Whether the collaborator is now idle.</param>
    /// <returns><see langword="true"/> when the cluster handled the update; <see langword="false"/> when it stayed unavailable.</returns>
    Task<bool> Idle(string groupKey, string sessionId, bool isIdle);

    /// <summary>
    /// Signals that this replica no longer holds any local sockets for a group, so the backplane can drop its
    /// subscription for it.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Release(string groupKey);
}
