// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Choreography.Hosting.for_CollaborationEndpointExtensions.when_connecting;

public class and_the_session_is_missing : given.an_endpoint
{
    protected override string QueryString => "?room=room-1";

    async Task Because() => await _endpoint(_context);

    [Fact] void should_answer_bad_request() => _context.Response.StatusCode.ShouldEqual(StatusCodes.Status400BadRequest);
    [Fact] void should_not_ask_the_resolver() => _resolver.DidNotReceive().Resolve(Arg.Any<HttpContext>(), Arg.Any<string>());
}
