// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// Who is connecting to a room, as the <see cref="ICollaborationParticipantResolver"/> of the application decided.
/// </summary>
/// <param name="GroupKey">
/// The key of the group of sockets that see each other. It must already be scoped to everything that must not leak
/// presence across, such as the tenant, so two organizations that use the same room name never share a group.
/// </param>
/// <param name="UserId">The identifier of the user, as the other collaborators see it on the wire.</param>
/// <param name="DisplayName">The name shown next to the user's cursor.</param>
/// <param name="HasAvatar">Whether the user has an avatar image; the others only ask for an image when there is one.</param>
public record CollaborationParticipant(string GroupKey, string UserId, string DisplayName, bool HasAvatar = false);
