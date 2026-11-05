// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// Defines how an application decides who is connecting to a room. The library knows nothing about identity or
/// tenancy; this is the single place where an application supplies both.
/// </summary>
/// <remarks>
/// The resolver is resolved from the request scope of the WebSocket upgrade request, so it can depend on scoped
/// services such as the current user and tenant.
/// </remarks>
public interface ICollaborationParticipantResolver
{
    /// <summary>
    /// Resolves the participant that is connecting to a room.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> of the WebSocket upgrade request.</param>
    /// <param name="room">The room the client asked to join, from the <c language="text">room</c> query parameter.</param>
    /// <returns>
    /// The <see cref="CollaborationParticipant"/>, or <see langword="null"/> when the caller is not allowed to join the
    /// room, in which case the upgrade is refused with <c language="text">401 Unauthorized</c>.
    /// </returns>
    Task<CollaborationParticipant?> Resolve(HttpContext context, string room);
}
