// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_CollaborationRoster.when_expiring;

public class and_the_session_was_active_recently : Specification
{
    given.a_manual_clock _clock = null!;
    CollaborationRoster _roster = null!;
    IReadOnlyList<Collaborator> _expired = [];

    void Establish()
    {
        _clock = new();
        _roster = new(_clock);
        var replica = Guid.NewGuid();
        _roster.Join(new Collaborator("session", Guid.NewGuid().ToString(), "Alice", "fff", 0, 0, "board", "surface", false), Guid.NewGuid(), replica);
        _roster.Refresh(replica, []);
        _clock.Advance(TimeSpan.FromSeconds(50));
        _roster.Presence("session", 5, 5, "board", "surface");
        _clock.Advance(TimeSpan.FromSeconds(50));
    }

    void Because() => _expired = _roster.Expire(TimeSpan.FromSeconds(60));

    [Fact] void should_not_expire_a_session_that_just_moved() => _expired.ShouldBeEmpty();
}
