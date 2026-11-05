// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Collaboration;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Choreography.Hosting.for_CollaborationEndpointExtensions.when_mapping;

public class and_the_path_is_configured : given.an_endpoint
{
    string _pattern = string.Empty;

    protected override void Configure(CollaborationOptions options) => options.Path = "/live/presence";

    void Because() => _pattern = ((IEndpointRouteBuilder)_app).DataSources
        .SelectMany(source => source.Endpoints)
        .OfType<RouteEndpoint>()
        .Single().RoutePattern.RawText!;

    [Fact] void should_map_the_configured_path() => _pattern.ShouldEqual("/live/presence");
}
