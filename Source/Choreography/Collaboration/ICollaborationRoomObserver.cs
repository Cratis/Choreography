// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// The push channel a replica registers with an <see cref="ICollaborationRoomGrain"/> so the grain can fan presence
/// events out to every replica holding live sockets for a room. Each callback carries the originating replica id so
/// the replica that produced an event can ignore the echo - it has already delivered that event to its own local
/// sockets directly (the low-latency fast path).
/// </summary>
public interface ICollaborationRoomObserver : IGrainObserver
{
    /// <summary>
    /// A collaborator joined the room.
    /// </summary>
    /// <param name="groupKey">The group key of the room.</param>
    /// <param name="collaborator">The collaborator that joined.</param>
    /// <param name="originPodId">The replica that produced the event.</param>
    void OnJoined(string groupKey, Collaborator collaborator, Guid originPodId);

    /// <summary>
    /// A collaborator left the room.
    /// </summary>
    /// <param name="groupKey">The group key of the room.</param>
    /// <param name="sessionId">The session that left.</param>
    /// <param name="originPodId">The replica that produced the event.</param>
    void OnLeft(string groupKey, string sessionId, Guid originPodId);

    /// <summary>
    /// A collaborator's cursor or area moved.
    /// </summary>
    /// <param name="groupKey">The group key of the room.</param>
    /// <param name="sessionId">The session that moved.</param>
    /// <param name="x">The cursor X position.</param>
    /// <param name="y">The cursor Y position.</param>
    /// <param name="area">The area of the application the collaborator is in.</param>
    /// <param name="surfaceId">The surface the collaborator is looking at.</param>
    /// <param name="originPodId">The replica that produced the event.</param>
    void OnPresence(string groupKey, string sessionId, double x, double y, string area, string surfaceId, Guid originPodId);

    /// <summary>
    /// A collaborator's idle state changed.
    /// </summary>
    /// <param name="groupKey">The group key of the room.</param>
    /// <param name="sessionId">The session whose idle state changed.</param>
    /// <param name="isIdle">Whether the collaborator is now idle.</param>
    /// <param name="originPodId">The replica that produced the event.</param>
    void OnIdle(string groupKey, string sessionId, bool isIdle, Guid originPodId);
}
