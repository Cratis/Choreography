// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_CollaborationRoster.when_leaving;

public class and_the_connection_matches : Specification
{
    CollaborationRoster _roster = null!;
    Guid _connectionId;
    bool _removed;

    void Establish()
    {
        _roster = new CollaborationRoster();
        _connectionId = Guid.NewGuid();
        _roster.Join(new Collaborator("session-a", Guid.NewGuid().ToString(), "Alice", "#fff", 0, 0, string.Empty, string.Empty, false), _connectionId, Guid.NewGuid());
    }

    void Because() => _removed = _roster.Leave("session-a", _connectionId);

    [Fact] void should_report_the_removal() => _removed.ShouldBeTrue();

    [Fact] void should_leave_the_roster_empty() => _roster.IsEmpty.ShouldBeTrue();
}
