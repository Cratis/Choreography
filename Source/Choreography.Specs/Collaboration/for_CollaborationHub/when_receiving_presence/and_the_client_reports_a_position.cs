// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Monads;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.when_receiving_presence;

public class and_the_client_reports_a_position : Specification
{
    ICollaborationBackplane _backplane = null!;
    CollaborationHub _hub = null!;
    readonly given.a_socket _sender = new();
    readonly given.a_blocking_socket _watcher = new();
    Task _watcherRun = Task.CompletedTask;
    JsonElement _presence;
    JsonElement _joined;

    async Task Establish()
    {
        _backplane = Substitute.For<ICollaborationBackplane>();
        _backplane.Join(Arg.Any<string>(), Arg.Any<Collaborator>(), Arg.Any<Guid>())
            .Returns(Task.FromResult<Result<IReadOnlyList<Collaborator>, CollaborationUnavailable>>(Array.Empty<Collaborator>()));
        _backplane.Presence("group", "sender", 10, 20, "board", "surface-1").Returns(true);
        _backplane.Leave(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>()).Returns(true);
        _hub = new(_backplane);

        _watcherRun = _hub.Run("group", "watcher", "user-2", "Alice", "00ff00", _watcher, CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (_hub.JoinedSessions("group").Count < 1)
        {
            await Task.Delay(10, timeout.Token);
        }

        _sender.Incoming.Enqueue("{\"type\":\"presence\",\"x\":10,\"y\":20,\"area\":\"board\",\"surfaceId\":\"surface-1\"}");
    }

    async Task Because()
    {
        await _hub.Run("group", "sender", "user-1", "Jane", "ff00ff", _sender, CancellationToken.None);
        _joined = JsonElement.Parse(_watcher.Sent.Single(message => message.Contains("\"joined\"")));
        _presence = JsonElement.Parse(_watcher.Sent.Single(message => message.Contains("\"presence\"")));
    }

    void Destroy()
    {
        _watcher.Abort();
        _watcherRun.GetAwaiter().GetResult();
    }

    [Fact] void should_tell_the_watcher_about_the_joined_collaborator_with_its_color() => _joined.GetProperty("collaborator").GetProperty("color").GetString().ShouldEqual("ff00ff");
    [Fact] void should_tell_the_watcher_the_collaborators_user_identifier_as_a_string() => _joined.GetProperty("collaborator").GetProperty("userId").GetString().ShouldEqual("user-1");
    [Fact] void should_broadcast_the_session() => _presence.GetProperty("sessionId").GetString().ShouldEqual("sender");
    [Fact] void should_broadcast_the_position() => (_presence.GetProperty("x").GetDouble(), _presence.GetProperty("y").GetDouble()).ShouldEqual((10d, 20d));
    [Fact] void should_broadcast_the_area() => _presence.GetProperty("area").GetString().ShouldEqual("board");
    [Fact] void should_broadcast_the_surface() => _presence.GetProperty("surfaceId").GetString().ShouldEqual("surface-1");
    [Fact] void should_relay_the_presence_to_the_backplane() => _backplane.Received(1).Presence("group", "sender", 10, 20, "board", "surface-1");
}
