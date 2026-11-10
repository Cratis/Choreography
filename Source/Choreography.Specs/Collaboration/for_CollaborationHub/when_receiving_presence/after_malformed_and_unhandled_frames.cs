// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.when_receiving_presence;

public class after_malformed_and_unhandled_frames : Specification
{
    readonly given.a_socket _socket = new();
    ICollaborationBackplane _backplane = null!;
    CollaborationHub _hub = null!;

    void Establish()
    {
        _backplane = Substitute.For<ICollaborationBackplane>();
        _backplane.Join(Arg.Any<string>(), Arg.Any<Collaborator>(), Arg.Any<Guid>())
            .Returns(Task.FromResult<Result<IReadOnlyList<Collaborator>, CollaborationUnavailable>>(Array.Empty<Collaborator>()));
        _backplane.Presence("group", "session", 10, 20, string.Empty, string.Empty).Returns(true);
        _backplane.Leave(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>()).Returns(true);
        _hub = new(_backplane);
        _socket.Incoming.Enqueue("{");
        _socket.Incoming.Enqueue("null");
        _socket.Incoming.Enqueue("{\"type\":\"pong\"}");
        _socket.Incoming.Enqueue("{\"type\":\"unknown\"}");
        _socket.Incoming.Enqueue("{\"type\":\"presence\",\"x\":10,\"y\":20}");
    }

    Task Because() => _hub.Run("group", "session", "user", "Jane", "ff00ff", _socket, CancellationToken.None);

    [Fact] void should_relay_the_following_valid_position() => _backplane.Received(1).Presence("group", "session", 10, 20, string.Empty, string.Empty);
    [Fact] void should_not_relay_the_unhandled_frames() => _backplane.Received(1).Presence(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<double>(), Arg.Any<double>(), Arg.Any<string>(), Arg.Any<string>());
}
