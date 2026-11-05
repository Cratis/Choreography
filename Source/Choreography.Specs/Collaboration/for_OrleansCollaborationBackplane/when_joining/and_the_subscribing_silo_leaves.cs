// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_OrleansCollaborationBackplane.when_joining;

public class and_the_subscribing_silo_leaves : given.a_backplane
{
    void Establish() => _grain.Subscribe(Arg.Any<ICollaborationRoomObserver>())
        .Returns(Task.FromException(new SiloUnavailableException("Silo left")), Task.CompletedTask);

    async Task Because() => _result = await _backplane.Join(Group, _collaborator, _connectionId);

    [Fact] void should_join_after_retrying() => _result.TryGetResult(out _).ShouldBeTrue();
    [Fact] void should_not_cache_the_failed_subscription() => _grain.Received(2).Subscribe(Arg.Any<ICollaborationRoomObserver>());
    [Fact] void should_resolve_the_grain_again() => _factory.Received(2).GetGrain<ICollaborationRoomGrain>(Group);
    [Fact] void should_back_off_before_retrying() => _delays.ShouldContainOnly(TimeSpan.FromMilliseconds(250));
    [Fact] void should_register_the_same_connection() => _grain.Received(1).Join(_collaborator, _connectionId, Arg.Any<Guid>());
}
