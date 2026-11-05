// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Collaboration;
using Microsoft.AspNetCore.Http;

namespace Cratis.Choreography.Hosting.for_CollaborationEndpointExtensions.when_connecting;

public class and_the_resolver_returns_a_participant : given.an_endpoint
{
    void Establish() => _resolver.Resolve(_context, "room-1")
        .Returns(new CollaborationParticipant("tenant-a|room-1", "user-1", "Jane", HasAvatar: true));

    async Task Because() => await _endpoint(_context);

    [Fact] void should_run_the_session_in_the_group_the_resolver_chose() => _hub.Received(1).Run("tenant-a|room-1", "session-1", "user-1", "Jane", "ff00ff", _socket, _context.RequestAborted, true);
    [Fact] void should_not_refuse_the_request() => _context.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
}
