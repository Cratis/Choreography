// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using System.Text.Json;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.when_a_session_joins_on_another_replica;

public class and_another_user_supplies_the_same_identifier : given.a_shared_room
{
    given.a_blocking_socket _first = null!;
    given.a_blocking_socket _second = null!;
    string _firstSession = null!;

    void Establish()
    {
        _first = Connect(_hub, "first-user");
        _firstSession = _joins.Single().SessionId;
    }

    async Task Because()
    {
        _second = Connect(_otherHub, "second-user");
        Connect(_otherHub, "second-user", _firstSession);
        var sender = new given.a_socket();
        sender.Incoming.Enqueue("{\"type\":\"presence\",\"x\":10,\"y\":20}");
        await _otherHub.Run("group", "session", "second-user", "Second", "fff", sender, CancellationToken.None);
    }

    [Fact] void should_keep_the_first_connection_open() => _first.State.ShouldEqual(WebSocketState.Open);
    [Fact] void should_keep_the_first_session_joined_on_its_replica() => _hub.JoinedSessions("group").ShouldContainOnly(_firstSession);
    [Fact] void should_preserve_the_first_users_presence_in_the_shared_roster() => _roster.All.Single(c => c.UserId == "first-user").X.ShouldEqual(0);
    [Fact] void should_scope_the_shared_identifier_to_each_user() => _joins[1].SessionId.ShouldNotEqual(_firstSession);
    [Fact] void should_scope_an_identifier_taken_from_the_roster() => _joins[2].SessionId.ShouldNotEqual(_firstSession);
    [Fact] void should_send_the_remote_user_in_the_roster() => JsonElement.Parse(_second.Sent[0]).GetProperty("collaborators").GetArrayLength().ShouldEqual(1);
    [Fact] void should_not_announce_that_the_first_user_left() => _leaves.ShouldNotContain(_firstSession);
}
