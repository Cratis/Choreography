// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_CollaborationRoster.when_leaving;

public class and_a_stale_connection_leaves_after_a_reconnect : Specification
{
    CollaborationRoster _roster = null!;
    Guid _staleConnectionId;
    Guid _freshConnectionId;
    bool _removed;

    void Establish()
    {
        _roster = new CollaborationRoster();
        _staleConnectionId = Guid.NewGuid();
        _freshConnectionId = Guid.NewGuid();

        // The same session reconnects onto another replica (a new connection id) before the old replica's leave arrives.
        _roster.Join(new Collaborator("session-a", Guid.NewGuid().ToString(), "Alice", "#fff", 0, 0, string.Empty, string.Empty, false), _staleConnectionId, Guid.NewGuid());
        _roster.Join(new Collaborator("session-a", Guid.NewGuid().ToString(), "Alice", "#fff", 0, 0, string.Empty, string.Empty, false), _freshConnectionId, Guid.NewGuid());
    }

    void Because() => _removed = _roster.Leave("session-a", _staleConnectionId);

    [Fact] void should_not_remove_the_reconnected_session() => _removed.ShouldBeFalse();

    [Fact] void should_keep_the_session_in_the_roster() => _roster.IsEmpty.ShouldBeFalse();
}
