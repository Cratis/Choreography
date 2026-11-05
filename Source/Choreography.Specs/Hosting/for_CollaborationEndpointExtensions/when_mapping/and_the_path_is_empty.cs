// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Collaboration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Hosting.for_CollaborationEndpointExtensions.when_mapping;

public class and_the_path_is_empty : Specification
{
    WebApplication _app = null!;
    Exception? _error;

    void Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddOptions<CollaborationOptions>().Configure(options => options.Path = " ");
        _app = builder.Build();
    }

    void Because() => _error = Catch.Exception(() => _app.MapCratisCollaboration());

    void Destroy() => _app.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Fact] void should_refuse_to_map() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
