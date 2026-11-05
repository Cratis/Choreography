// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Collaboration;
using Microsoft.AspNetCore.Http;

namespace Cratis.Choreography.Hosting.for_CollaborationServiceCollectionExtensions.given;

public class FakeParticipantResolver : ICollaborationParticipantResolver
{
    public Task<CollaborationParticipant?> Resolve(HttpContext context, string room) =>
        Task.FromResult<CollaborationParticipant?>(new CollaborationParticipant(room, "user", "User"));
}
