// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_OrleansCollaborationBackplane.when_joining;

public class and_the_failure_is_not_transient : given.a_backplane
{
    readonly Exception _failure = new("Unexpected failure");
    Exception? _caught;

    void Establish() => _grain.Subscribe(Arg.Any<ICollaborationRoomObserver>()).Returns(Task.FromException(_failure));

    async Task Because() => _caught = await Cratis.Specifications.Catch.Exception(() => _backplane.Join(Group, _collaborator, _connectionId));

    [Fact] void should_preserve_the_unexpected_failure() => _caught.ShouldBeSame(_failure);
    [Fact] void should_not_retry_a_programming_error() => _delays.ShouldBeEmpty();
}
