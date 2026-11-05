// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Runtime.Messaging;

namespace Cratis.Choreography.Collaboration.for_OrleansCollaborationBackplane.when_joining;

public class and_the_cluster_stays_unavailable : given.a_backplane
{
    void Establish() => _grain.Subscribe(Arg.Any<ICollaborationRoomObserver>())
        .Returns(_ => Task.FromException(new ConnectionFailedException("Silo left")));

    async Task Because() => _result = await _backplane.Join(Group, _collaborator, _connectionId);

    [Fact] void should_return_an_expected_unavailable_result() => _result.TryGetResult(out _).ShouldBeFalse();
    [Fact] void should_stop_after_six_attempts() => _grain.Received(6).Subscribe(Arg.Any<ICollaborationRoomObserver>());
    [Fact] void should_keep_the_backoff_under_ten_seconds() => _delays.Sum(delay => delay.TotalSeconds).ShouldBeLessThan(10d);
    [Fact] void should_never_claim_the_session_joined() => _grain.DidNotReceive().Join(Arg.Any<Collaborator>(), Arg.Any<Guid>(), Arg.Any<Guid>());
}
