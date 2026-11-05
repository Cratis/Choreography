// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Collaboration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Hosting.for_CollaborationServiceCollectionExtensions.when_adding_orleans_collaboration;

public class and_it_is_added_before_collaboration : Specification
{
    ServiceProvider _provider = null!;
    ICollaborationBackplane _backplane = null!;

    void Establish()
    {
        var factory = Substitute.For<IGrainFactory>();
        factory.CreateObjectReference<ICollaborationRoomObserver>(Arg.Any<ICollaborationRoomObserver>())
            .Returns(Substitute.For<ICollaborationRoomObserver>());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(factory);
        services.AddCratisCollaborationOrleans();
        services.AddCratisCollaboration<given.FakeParticipantResolver>();
        _provider = services.BuildServiceProvider();
    }

    void Because() => _backplane = _provider.GetRequiredService<ICollaborationBackplane>();

    void Destroy() => _provider.Dispose();

    [Fact] void should_keep_the_orleans_backplane() => _backplane.ShouldBeOfExactType<OrleansCollaborationBackplane>();
}
