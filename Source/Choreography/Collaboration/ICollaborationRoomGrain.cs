// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// The cluster-wide authority for one room's live collaboration presence, keyed by the group key the
/// <see cref="ICollaborationParticipantResolver"/> produced. It holds the authoritative roster across every replica
/// and fans presence changes out to all replicas that hold sockets for the room, so collaboration is correct when the
/// application runs at more than one replica. Presence is ephemeral - the grain deactivates once its roster empties -
/// so there is no persisted state.
/// </summary>
[GrainInterfaceType("Cratis.Choreography.Collaboration.ICollaborationRoomGrain")]
public interface ICollaborationRoomGrain : IGrainWithStringKey
{
    /// <summary>
    /// Registers a replica's observer so it receives presence events for this room.
    /// </summary>
    /// <param name="observer">The replica observer.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Subscribe(ICollaborationRoomObserver observer);

    /// <summary>
    /// Removes a replica's observer.
    /// </summary>
    /// <param name="observer">The replica observer.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Unsubscribe(ICollaborationRoomObserver observer);

    /// <summary>
    /// Adds (or replaces on reconnect) a session in the roster and notifies the other replicas.
    /// </summary>
    /// <param name="collaborator">The joining collaborator.</param>
    /// <param name="connectionId">A per-connection token used to make a cross-replica reconnect race-safe: a
    /// later <see cref="Leave"/> from a stale connection is ignored when the roster already holds a newer one.</param>
    /// <param name="originPodId">The replica producing the join.</param>
    /// <returns>The collaborators currently present on OTHER replicas (the caller merges these with its own
    /// local roster to build the joining client's snapshot).</returns>
    Task<IReadOnlyList<Collaborator>> Join(Collaborator collaborator, Guid connectionId, Guid originPodId);

    /// <summary>
    /// Removes a session from the roster and notifies the other replicas, unless a newer connection for the same
    /// session has since joined (identified by <paramref name="connectionId"/>).
    /// </summary>
    /// <param name="sessionId">The session leaving.</param>
    /// <param name="connectionId">The token from the corresponding <see cref="Join"/>.</param>
    /// <param name="originPodId">The replica producing the leave.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Leave(string sessionId, Guid connectionId, Guid originPodId);

    /// <summary>
    /// Updates a session's cursor and area in the roster and notifies the other replicas.
    /// </summary>
    /// <param name="sessionId">The session that moved.</param>
    /// <param name="x">The cursor X position.</param>
    /// <param name="y">The cursor Y position.</param>
    /// <param name="area">The area the collaborator is in.</param>
    /// <param name="surfaceId">The surface the collaborator is looking at.</param>
    /// <param name="originPodId">The replica producing the update.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Presence(string sessionId, double x, double y, string area, string surfaceId, Guid originPodId);

    /// <summary>
    /// Updates a session's idle state in the roster and notifies the other replicas.
    /// </summary>
    /// <param name="sessionId">The session whose idle state changed.</param>
    /// <param name="isIdle">Whether the collaborator is now idle.</param>
    /// <param name="originPodId">The replica producing the update.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Idle(string sessionId, bool isIdle, Guid originPodId);

    /// <summary>
    /// Vouches for the sessions a replica still holds an open connection for, keeping them in the roster. Sessions of a
    /// replica that stops vouching (killed, rolled out, unreachable) expire, so no ghost outlives its replica.
    /// </summary>
    /// <param name="originPodId">The replica that is vouching.</param>
    /// <param name="sessionIds">The sessions the replica still has an open connection for.</param>
    /// <returns>The sessions the roster did not know (for example because it moved to another silo), which the replica re-establishes.</returns>
    Task<IReadOnlyList<string>> Refresh(Guid originPodId, IReadOnlyList<string> sessionIds);
}
