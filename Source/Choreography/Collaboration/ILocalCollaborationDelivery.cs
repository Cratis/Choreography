// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// The channel an <see cref="ICollaborationBackplane"/> uses to hand a presence event that originated on ANOTHER
/// replica to this replica's <see cref="CollaborationHub"/>, which then fans it out to its local sockets. The hub
/// implements this and attaches itself to the backplane; events produced locally never travel this way (the hub
/// delivers those to its own sockets directly).
/// </summary>
public interface ILocalCollaborationDelivery
{
    /// <summary>
    /// Asks every local socket of a group to reconnect, which re-establishes their presence in the roster. Used
    /// when the shared roster may have been lost.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeliverReconnect(string groupKey);

    /// <summary>
    /// Delivers a remote "joined" to local sockets.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <param name="collaborator">The collaborator that joined.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeliverJoined(string groupKey, Collaborator collaborator);

    /// <summary>
    /// Delivers a remote "left" to local sockets.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <param name="sessionId">The session that left.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeliverLeft(string groupKey, string sessionId);

    /// <summary>
    /// Delivers a remote "presence" to local sockets.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <param name="sessionId">The session that moved.</param>
    /// <param name="x">The cursor X position.</param>
    /// <param name="y">The cursor Y position.</param>
    /// <param name="area">The area the collaborator is in.</param>
    /// <param name="surfaceId">The surface the collaborator is looking at.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeliverPresence(string groupKey, string sessionId, double x, double y, string area, string surfaceId);

    /// <summary>
    /// Delivers a remote "idle" to local sockets.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <param name="sessionId">The session whose idle state changed.</param>
    /// <param name="isIdle">Whether the collaborator is now idle.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeliverIdle(string groupKey, string sessionId, bool isIdle);

    /// <summary>
    /// Gets the sessions of a group this replica holds an open, joined connection for.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <returns>The session identifiers.</returns>
    IReadOnlyList<string> JoinedSessions(string groupKey);

    /// <summary>
    /// Asks the local connection of a session to reconnect so it re-establishes its presence in the roster.
    /// </summary>
    /// <param name="groupKey">The group key.</param>
    /// <param name="sessionId">The session to rejoin.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeliverRejoin(string groupKey, string sessionId);
}
