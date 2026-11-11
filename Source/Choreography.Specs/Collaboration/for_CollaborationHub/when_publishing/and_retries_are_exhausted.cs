// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;
using Cratis.Monads;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.when_publishing;

public class and_retries_are_exhausted : Specification
{
    ICollaborationBackplane _backplane = null!;
    CollaborationHub _hub = null!;
    readonly given.a_socket _socket = new();
    Exception? _exception;

    void Establish()
    {
        _backplane = Substitute.For<ICollaborationBackplane>();
        _backplane.Join("group", Arg.Any<Collaborator>(), Arg.Any<Guid>())
            .Returns<Result<IReadOnlyList<Collaborator>, CollaborationUnavailable>>(new List<Collaborator>());
        _backplane.Presence("group", Arg.Any<string>(), 10, 20, "board", "surface").Returns(false);
        _socket.Incoming.Enqueue("{\"type\":\"presence\",\"x\":10,\"y\":20,\"area\":\"board\",\"surfaceId\":\"model\"}");
        _hub = new(_backplane);
    }

    async Task Because() => _exception = await Cratis.Specifications.Catch.Exception(() =>
        _hub.Run("group", "session", Guid.NewGuid().ToString(), "Alice", "fff", _socket, CancellationToken.None));

    [Fact] void should_complete_without_an_unhandled_exception() => _exception.ShouldBeNull();
    [Fact] void should_request_a_rejoin() => _socket.RequestedClose.ShouldEqual(WebSocketCloseStatus.EndpointUnavailable);
    [Fact] void should_release_the_connection() => _backplane.Received(1).Leave("group", Arg.Is<string>(session => session != "session"), Arg.Any<Guid>());
}
