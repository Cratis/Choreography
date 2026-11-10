// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Monads;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.when_a_session_joins;

public class and_it_is_alone : Specification
{
    ICollaborationBackplane _backplane = null!;
    CollaborationHub _hub = null!;
    readonly given.a_socket _socket = new();
    JsonElement _roster;

    void Establish()
    {
        _backplane = Substitute.For<ICollaborationBackplane>();
        _backplane.Join(Arg.Any<string>(), Arg.Any<Collaborator>(), Arg.Any<Guid>())
            .Returns(Task.FromResult<Result<IReadOnlyList<Collaborator>, CollaborationUnavailable>>(Array.Empty<Collaborator>()));
        _backplane.Leave(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>()).Returns(true);
        _hub = new(_backplane);
    }

    async Task Because()
    {
        await _hub.Run("group", "session", "user-1", "Jane", "ff00ff", _socket, CancellationToken.None, hasAvatar: true);
        _roster = JsonElement.Parse(_socket.Sent[0]);
    }

    [Fact] void should_send_a_roster_message_first() => _roster.GetProperty("type").GetString().ShouldEqual("roster");
    [Fact] void should_tell_the_client_its_own_user_identifier() => _roster.GetProperty("userId").GetString().ShouldEqual("user-1");
    [Fact] void should_send_an_empty_collaborator_list() => _roster.GetProperty("collaborators").GetArrayLength().ShouldEqual(0);
}
