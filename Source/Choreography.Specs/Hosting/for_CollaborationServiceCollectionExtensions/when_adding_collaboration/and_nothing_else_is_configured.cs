// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Collaboration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Hosting.for_CollaborationServiceCollectionExtensions.when_adding_collaboration;

public class and_nothing_else_is_configured : Specification
{
    ServiceProvider _provider = null!;
    ICollaborationHub _hub = null!;
    ICollaborationBackplane _backplane = null!;
    ICollaborationParticipantResolver _resolver = null!;
    CollaborationOptions _options = null!;

    void Establish()
    {
        var services = new ServiceCollection();
        services.AddCratisCollaboration<given.FakeParticipantResolver>();
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    void Because()
    {
        _hub = _provider.GetRequiredService<ICollaborationHub>();
        _backplane = _provider.GetRequiredService<ICollaborationBackplane>();
        using var scope = _provider.CreateScope();
        _resolver = scope.ServiceProvider.GetRequiredService<ICollaborationParticipantResolver>();
        _options = _provider.GetRequiredService<IOptions<CollaborationOptions>>().Value;
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_resolve_the_hub() => _hub.ShouldBeOfExactType<CollaborationHub>();
    [Fact] void should_use_the_local_backplane() => _backplane.ShouldBeOfExactType<LocalCollaborationBackplane>();
    [Fact] void should_resolve_the_applications_resolver() => _resolver.ShouldBeOfExactType<given.FakeParticipantResolver>();
    [Fact] void should_use_the_default_path() => _options.Path.ShouldEqual(CollaborationOptions.DefaultPath);
    [Fact] void should_share_one_hub() => _provider.GetRequiredService<ICollaborationHub>().ShouldBeSame(_hub);
}
