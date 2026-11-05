// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Collaboration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cratis.Choreography.Hosting.for_CollaborationServiceCollectionExtensions.when_adding_orleans_collaboration;

public class and_the_grain_factory_is_available : Specification
{
    ServiceProvider _provider = null!;
    ICollaborationBackplane _backplane = null!;
    ICollaborationHub _hub = null!;
    IEnumerable<IHostedService> _hostedServices = [];

    void Establish()
    {
        var factory = Substitute.For<IGrainFactory>();
        factory.CreateObjectReference<ICollaborationRoomObserver>(Arg.Any<ICollaborationRoomObserver>())
            .Returns(Substitute.For<ICollaborationRoomObserver>());

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(factory);
        services.AddCratisCollaboration<given.FakeParticipantResolver>();
        services.AddCratisCollaborationOrleans();
        _provider = services.BuildServiceProvider();
    }

    void Because()
    {
        _hub = _provider.GetRequiredService<ICollaborationHub>();
        _backplane = _provider.GetRequiredService<ICollaborationBackplane>();
        _hostedServices = _provider.GetServices<IHostedService>();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_swap_in_the_orleans_backplane() => _backplane.ShouldBeOfExactType<OrleansCollaborationBackplane>();
    [Fact] void should_run_the_same_backplane_as_a_hosted_service() => _hostedServices.OfType<OrleansCollaborationBackplane>().Single().ShouldBeSame(_backplane);
    [Fact] void should_give_the_hub_the_orleans_backplane() => _hub.ShouldBeOfExactType<CollaborationHub>();
}
