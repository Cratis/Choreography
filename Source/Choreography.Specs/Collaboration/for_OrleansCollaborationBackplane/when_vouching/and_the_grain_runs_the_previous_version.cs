// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_OrleansCollaborationBackplane.when_vouching;

public class and_the_grain_runs_the_previous_version : given.a_backplane
{
    Exception? _exception;

    async Task Establish()
    {
        await _backplane.Join(Group, _collaborator, _connectionId);
        _delivery.JoinedSessions(Group).Returns(["session"]);
        _grain.Refresh(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<string>>())
            .Returns(_ => Task.FromException<IReadOnlyList<string>>(new InvalidOperationException("Unknown method")));
    }

    async Task Because() => _exception = await Cratis.Specifications.Catch.Exception(() => _backplane.Vouch(Group));

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_not_have_any_session_reconnect() => _delivery.DidNotReceive().DeliverRejoin(Group, Arg.Any<string>());
    [Fact] void should_not_mistake_it_for_a_lost_cluster() => _delivery.DidNotReceive().DeliverReconnect(Group);
}
