// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Choreography.Collaboration.for_CollaborationHub.when_a_session_joins_on_another_replica;

public class and_this_replica_still_holds_a_connection_for_it : Specification
{
    ICollaborationBackplane _backplane = null!;
    CollaborationHub _hub = null!;
    readonly given.a_blocking_socket _stale = new();
    readonly given.a_blocking_socket _viewer = new();
    Task _staleRun = Task.CompletedTask;
    Task _viewerRun = Task.CompletedTask;
    Collaborator _rejoined = null!;

    async Task Establish()
    {
        _backplane = Substitute.For<ICollaborationBackplane>();
        _backplane.Join(Arg.Any<string>(), Arg.Any<Collaborator>(), Arg.Any<Guid>())
            .Returns(Task.FromResult<Result<IReadOnlyList<Collaborator>, CollaborationUnavailable>>(Array.Empty<Collaborator>()));
        _backplane.Leave(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>()).Returns(true);
        _hub = new(_backplane);

        // The session's old, half-open connection lives here; a viewer on this replica watches the board.
        var user = Guid.NewGuid().ToString();
        _staleRun = _hub.Run("group", "session", user, "Einar", "fff", _stale, CancellationToken.None);
        var session = _hub.JoinedSessions("group").Single();
        _viewerRun = _hub.Run("group", "viewer", Guid.NewGuid().ToString(), "Alice", "fff", _viewer, CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (_hub.JoinedSessions("group").Count < 2)
        {
            await Task.Delay(10, timeout.Token);
        }

        _rejoined = new Collaborator(session, user, "Einar", "fff", 0, 0, "board", "surface", false);
    }

    async Task Because()
    {
        await _hub.DeliverJoined("group", _rejoined);
        await _staleRun;
    }

    void Destroy()
    {
        _viewer.Abort();
        _viewerRun.GetAwaiter().GetResult();
    }

    [Fact] void should_drop_the_superseded_connection() => _stale.Aborted.ShouldBeTrue();
    [Fact] void should_not_remove_the_newer_roster_entry() => _backplane.DidNotReceive().Leave("group", _rejoined.SessionId, Arg.Any<Guid>());
    [Fact] void should_not_tell_the_viewer_that_the_session_left() => _viewer.Sent.Exists(message => message.Contains("\"left\"")).ShouldBeFalse();
    [Fact] void should_tell_the_viewer_the_session_joined() => _viewer.Sent.Exists(message => message.Contains("\"joined\"")).ShouldBeTrue();
    [Fact] void should_only_hold_the_viewer() => _hub.JoinedSessions("group").Count.ShouldEqual(1);
}
