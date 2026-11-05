// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Collaboration;
using Microsoft.AspNetCore.Http;

namespace Cratis.Choreography.Hosting.for_CollaborationEndpointExtensions.when_connecting;

public class and_the_resolver_returns_no_participant : given.an_endpoint
{
    void Establish() => _resolver.Resolve(_context, "room-1").Returns((CollaborationParticipant?)null);

    async Task Because() => await _endpoint(_context);

    [Fact] void should_answer_unauthorized() => _context.Response.StatusCode.ShouldEqual(StatusCodes.Status401Unauthorized);
    [Fact] void should_not_accept_the_socket() => _hub.DidNotReceive().Run(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<System.Net.WebSockets.WebSocket>(), Arg.Any<CancellationToken>(), Arg.Any<bool>());
}
