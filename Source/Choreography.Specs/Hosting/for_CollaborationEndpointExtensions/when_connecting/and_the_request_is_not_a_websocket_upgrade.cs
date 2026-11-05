// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Choreography.Hosting.for_CollaborationEndpointExtensions.when_connecting;

public class and_the_request_is_not_a_websocket_upgrade : given.an_endpoint
{
    protected override bool IsWebSocketRequest => false;

    async Task Because() => await _endpoint(_context);

    [Fact] void should_answer_bad_request() => _context.Response.StatusCode.ShouldEqual(StatusCodes.Status400BadRequest);
    [Fact] void should_not_ask_the_resolver() => _resolver.DidNotReceive().Resolve(Arg.Any<HttpContext>(), Arg.Any<string>());
}
