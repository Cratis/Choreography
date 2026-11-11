// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.when_a_session_joins;

public class and_the_same_user_reconnects : given.a_shared_room
{
    given.a_blocking_socket _first = null!;
    given.a_blocking_socket _successor = null!;

    void Establish() => _first = Connect(_hub, "user");

    async Task Because()
    {
        _successor = Connect(_hub, "user");
        _first.Abort();
        await _runs[0].WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_close_the_earlier_connection() => _first.RequestedClose.ShouldEqual(WebSocketCloseStatus.NormalClosure);
    [Fact] void should_keep_the_successor_open() => _successor.State.ShouldEqual(WebSocketState.Open);
    [Fact] void should_reuse_the_same_effective_identifier() => _joins[1].SessionId.ShouldEqual(_joins[0].SessionId);
    [Fact] void should_keep_the_successor_joined() => _hub.JoinedSessions("group").ShouldContainOnly(_joins[1].SessionId);
    [Fact] void should_not_remove_the_successor_from_the_shared_roster() => _roster.All.ShouldContainOnly(_joins[1]);
    [Fact] void should_not_announce_a_leave_for_the_earlier_connection() => _leaves.ShouldBeEmpty();
}
