// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.WebSockets;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.when_joining;

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
            .Returns(new CollaborationUnavailable());
        _hub = new(_backplane);
    }

    async Task Because() => _exception = await Cratis.Specifications.Catch.Exception(() =>
        _hub.Run("group", "session", Guid.NewGuid().ToString(), "Alice", "fff", _socket, CancellationToken.None));

    [Fact] void should_complete_without_an_unhandled_exception() => _exception.ShouldBeNull();
    [Fact] void should_close_with_an_expected_reconnect_status() => _socket.RequestedClose.ShouldEqual(WebSocketCloseStatus.EndpointUnavailable);
    [Fact] void should_end_the_receive_loop() => _socket.Aborted.ShouldBeTrue();
    [Fact] void should_remove_any_partial_registration() => _backplane.Received(1).Leave("group", "session", Arg.Any<Guid>());
    [Fact] void should_release_the_failed_join_group() => _backplane.Received(1).Release("group");
    [Fact] void should_not_send_a_successful_roster() => _socket.Sent.ShouldBeEmpty();
}
