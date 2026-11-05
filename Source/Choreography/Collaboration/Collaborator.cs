// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// The live state of one collaborator session within a room.
/// </summary>
/// <param name="SessionId">The collaborator's session (browser tab) identifier.</param>
/// <param name="UserId">The collaborator's user identifier, as the <see cref="ICollaborationParticipantResolver"/> returned it.</param>
/// <param name="DisplayName">The collaborator's display name.</param>
/// <param name="Color">The collaborator's cursor color, as a hex value without the leading <c language="text">#</c>.</param>
/// <param name="X">The cursor X position in the coordinates of the surface the collaborator is on.</param>
/// <param name="Y">The cursor Y position in the coordinates of the surface the collaborator is on.</param>
/// <param name="Area">The area of the application the collaborator is in.</param>
/// <param name="SurfaceId">The identifier of the surface within the area the collaborator is looking at, such as a board or a document.</param>
/// <param name="IsIdle">Whether the collaborator has been inactive beyond the idle threshold.</param>
/// <param name="HasAvatar">Whether the collaborator has an avatar image the other collaborators can ask for.</param>
[GenerateSerializer]
public record Collaborator(
    [property: Id(0)] string SessionId,
    [property: Id(1)] string UserId,
    [property: Id(2)] string DisplayName,
    [property: Id(3)] string Color,
    [property: Id(4)] double X,
    [property: Id(5)] double Y,
    [property: Id(6)] string Area,
    [property: Id(7)] string SurfaceId,
    [property: Id(8)] bool IsIdle,
    [property: Id(9)] bool HasAvatar = false);
