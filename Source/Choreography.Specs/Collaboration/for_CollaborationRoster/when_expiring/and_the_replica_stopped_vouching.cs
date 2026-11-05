// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_CollaborationRoster.when_expiring;

public class and_the_replica_stopped_vouching : Specification
{
    given.a_manual_clock _clock = null!;
    CollaborationRoster _roster = null!;
    Guid _deadReplica;
    Guid _liveReplica;
    IReadOnlyList<Collaborator> _expired = [];

    void Establish()
    {
        _clock = new();
        _roster = new(_clock);
        _deadReplica = Guid.NewGuid();
        _liveReplica = Guid.NewGuid();

        // Einar signed in on a replica that was then OOM-killed: its entry was last seen idle and nobody ever left it.
        _roster.Join(new Collaborator("ghost", Guid.NewGuid().ToString(), "Einar", "fff", 0, 0, "board", "surface", true), Guid.NewGuid(), _deadReplica);
        _roster.Join(new Collaborator("alive", Guid.NewGuid().ToString(), "Alice", "fff", 0, 0, "board", "surface", false), Guid.NewGuid(), _liveReplica);
        _roster.Refresh(_deadReplica, ["ghost"]);
        _roster.Refresh(_liveReplica, ["alive"]);

        _clock.Advance(TimeSpan.FromSeconds(45));
        _roster.Refresh(_liveReplica, ["alive"]);
        _clock.Advance(TimeSpan.FromSeconds(30));
        _roster.Refresh(_liveReplica, ["alive"]);
    }

    void Because() => _expired = _roster.Expire(TimeSpan.FromSeconds(60));

    [Fact] void should_expire_the_session_of_the_replica_that_stopped_vouching() => _expired.Select(c => c.SessionId).ShouldContainOnly("ghost");
    [Fact] void should_keep_the_session_of_the_replica_that_still_vouches() => _roster.All.Select(c => c.SessionId).ShouldContainOnly("alive");
}
