// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_CollaborationRoster.when_refreshing;

public class and_the_roster_lost_a_session : Specification
{
    CollaborationRoster _roster = null!;
    Guid _replica;
    IReadOnlyList<string> _unknown = [];

    void Establish()
    {
        _roster = new();
        _replica = Guid.NewGuid();
        _roster.Join(new Collaborator("known", Guid.NewGuid().ToString(), "Alice", "fff", 0, 0, string.Empty, string.Empty, false), Guid.NewGuid(), _replica);

        // "moved" is held by this replica but the roster does not have it (a new grain activation), and "elsewhere" now belongs to another replica.
        _roster.Join(new Collaborator("elsewhere", Guid.NewGuid().ToString(), "Bob", "fff", 0, 0, string.Empty, string.Empty, false), Guid.NewGuid(), Guid.NewGuid());
    }

    void Because() => _unknown = _roster.Refresh(_replica, ["known", "moved", "elsewhere"]);

    [Fact] void should_report_the_sessions_the_roster_does_not_hold_for_this_replica() => _unknown.ShouldContainOnly("moved", "elsewhere");
}
