// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Routing;

namespace Cratis.Choreography.Hosting.for_CollaborationEndpointExtensions.when_mapping;

public class and_the_path_is_not_configured : given.an_endpoint
{
    string _pattern = string.Empty;

    void Because() => _pattern = ((IEndpointRouteBuilder)_app).DataSources
        .SelectMany(source => source.Endpoints)
        .OfType<RouteEndpoint>()
        .Single().RoutePattern.RawText!;

    [Fact] void should_map_the_default_path() => _pattern.ShouldEqual("/ws/collaboration");
}
