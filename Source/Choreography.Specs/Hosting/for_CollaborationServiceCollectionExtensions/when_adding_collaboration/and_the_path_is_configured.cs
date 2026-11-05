// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Collaboration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Hosting.for_CollaborationServiceCollectionExtensions.when_adding_collaboration;

public class and_the_path_is_configured : Specification
{
    ServiceProvider _provider = null!;
    CollaborationOptions _options = null!;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddCratisCollaboration<given.FakeParticipantResolver>(options => options.Path = "/live");
        _provider = services.BuildServiceProvider();
    }

    void Because() => _options = _provider.GetRequiredService<IOptions<CollaborationOptions>>().Value;

    void Destroy() => _provider.Dispose();

    [Fact] void should_use_the_configured_path() => _options.Path.ShouldEqual("/live");
}
