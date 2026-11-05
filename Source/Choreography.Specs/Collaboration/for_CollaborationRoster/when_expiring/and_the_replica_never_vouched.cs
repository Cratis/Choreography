// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_CollaborationRoster.when_expiring;

public class and_the_replica_never_vouched : Specification
{
    given.a_manual_clock _clock = null!;
    CollaborationRoster _roster = null!;
    IReadOnlyList<Collaborator> _expired = [];

    void Establish()
    {
        _clock = new();
        _roster = new(_clock);

        // A replica still running the version before vouching existed: its sessions can only be removed by its own leave.
        _roster.Join(new Collaborator("old-replica-session", Guid.NewGuid().ToString(), "Bob", "fff", 0, 0, "board", "surface", false), Guid.NewGuid(), Guid.NewGuid());
        _clock.Advance(TimeSpan.FromHours(1));
    }

    void Because() => _expired = _roster.Expire(TimeSpan.FromSeconds(60));

    [Fact] void should_not_expire_anything() => _expired.ShouldBeEmpty();
    [Fact] void should_keep_the_session() => _roster.All.Count.ShouldEqual(1);
}
