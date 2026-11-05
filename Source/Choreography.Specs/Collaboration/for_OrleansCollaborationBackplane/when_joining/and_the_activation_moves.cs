// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_OrleansCollaborationBackplane.when_joining;

public class and_the_activation_moves : given.a_backplane
{
    ICollaborationRoomGrain _replacement = null!;

    void Establish()
    {
        _replacement = Substitute.For<ICollaborationRoomGrain>();
        _replacement.Join(Arg.Any<Collaborator>(), Arg.Any<Guid>(), Arg.Any<Guid>()).Returns([]);
        _factory.GetGrain<ICollaborationRoomGrain>(Group).Returns(_grain, _replacement);
        _grain.Join(Arg.Any<Collaborator>(), Arg.Any<Guid>(), Arg.Any<Guid>())
            .Returns(_ => Task.FromException<IReadOnlyList<Collaborator>>(new SiloUnavailableException("The activation moved to another silo")));
    }

    async Task Because() => _result = await _backplane.Join(Group, _collaborator, _connectionId);

    [Fact] void should_join_the_replacement() => _result.TryGetResult(out _).ShouldBeTrue();
    [Fact] void should_subscribe_on_the_new_activation() => _replacement.Received(1).Subscribe(Arg.Any<ICollaborationRoomObserver>());
    [Fact] void should_join_using_the_original_connection_token() => _replacement.Received(1).Join(_collaborator, _connectionId, Arg.Any<Guid>());
    [Fact] void should_request_a_roster_resync_for_existing_sockets() => _delivery.Received(1).DeliverReconnect(Group);
}
