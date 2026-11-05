// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_OrleansCollaborationBackplane.when_vouching;

public class and_the_grain_lost_a_session : given.a_backplane
{
    async Task Establish()
    {
        await _backplane.Join(Group, _collaborator, _connectionId);
        _delivery.JoinedSessions(Group).Returns(["session", "lost"]);
        _grain.Refresh(Arg.Any<Guid>(), Arg.Any<IReadOnlyList<string>>()).Returns(Task.FromResult<IReadOnlyList<string>>(["lost"]));
    }

    async Task Because() => await _backplane.Vouch(Group);

    [Fact] void should_vouch_for_the_sessions_the_replica_holds() => _grain.Received(1).Refresh(Arg.Any<Guid>(), Arg.Is<IReadOnlyList<string>>(sessions => sessions.SequenceEqual(new[] { "session", "lost" })));
    [Fact] void should_have_the_lost_session_reconnect() => _delivery.Received(1).DeliverRejoin(Group, "lost");
    [Fact] void should_not_have_the_known_session_reconnect() => _delivery.DidNotReceive().DeliverRejoin(Group, "session");
}
