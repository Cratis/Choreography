// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.when_a_session_joins;

public class and_the_same_identifier_is_used_in_another_group : given.a_shared_room
{
    CollaborationHub _local = null!;
    given.a_blocking_socket _first = null!;
    given.a_blocking_socket _second = null!;

    void Establish()
    {
        _local = new(new LocalCollaborationBackplane());
        _first = Connect(_local, "user");
    }

    void Because() => _second = Connect(_local, "user", group: "other-group");

    [Fact] void should_keep_the_first_group_connected() => _first.State.ShouldEqual(WebSocketState.Open);
    [Fact] void should_keep_the_second_group_connected() => _second.State.ShouldEqual(WebSocketState.Open);
    [Fact] void should_keep_one_session_in_each_group() => (_local.JoinedSessions("group").Count, _local.JoinedSessions("other-group").Count).ShouldEqual((1, 1));
    [Fact] void should_not_send_the_other_groups_join() => _first.Sent.Count.ShouldEqual(1);
}
