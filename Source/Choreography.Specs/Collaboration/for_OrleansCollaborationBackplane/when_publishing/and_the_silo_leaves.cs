// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_OrleansCollaborationBackplane.when_publishing;

public class and_the_silo_leaves : given.a_backplane
{
    bool _published;

    async Task Establish()
    {
        await _backplane.Join(Group, _collaborator, _connectionId);
        _grain.Presence("session", 10, 20, "board", "surface", Arg.Any<Guid>())
            .Returns(Task.FromException(new SiloUnavailableException("Silo left")), Task.CompletedTask);
    }

    async Task Because() => _published = await _backplane.Presence(Group, "session", 10, 20, "board", "surface");

    [Fact] void should_retry_the_presence_update() => _published.ShouldBeTrue();
    [Fact] void should_reestablish_the_subscription() => _grain.Received(2).Subscribe(Arg.Any<ICollaborationRoomObserver>());
    [Fact] void should_request_a_roster_resync_for_existing_sockets() => _delivery.Received(1).DeliverReconnect(Group);
}
