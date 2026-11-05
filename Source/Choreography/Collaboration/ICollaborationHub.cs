// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// Defines the hub that tracks live collaborator connections per group and broadcasts presence over two-way
/// WebSockets.
/// </summary>
public interface ICollaborationHub
{
    /// <summary>
    /// Runs a collaborator's WebSocket session until the socket closes: joins the group, pumps presence messages to
    /// the other members, and leaves (with a broadcast) on disconnect.
    /// </summary>
    /// <param name="groupKey">The group key the collaborator joins, already scoped by the application.</param>
    /// <param name="sessionId">The collaborator's session identifier.</param>
    /// <param name="userId">The collaborator's user identifier.</param>
    /// <param name="displayName">The collaborator's display name.</param>
    /// <param name="color">The collaborator's cursor color, as a hex value without the leading <c language="text">#</c>.</param>
    /// <param name="socket">The accepted WebSocket.</param>
    /// <param name="cancellationToken">Token cancelled when the host shuts down.</param>
    /// <param name="hasAvatar">Whether the collaborator has an avatar; the others only ask for an image when there is one.</param>
    /// <returns>A <see cref="Task"/> completing when the session ends.</returns>
    Task Run(string groupKey, string sessionId, string userId, string displayName, string color, WebSocket socket, CancellationToken cancellationToken, bool hasAvatar = false);
}
