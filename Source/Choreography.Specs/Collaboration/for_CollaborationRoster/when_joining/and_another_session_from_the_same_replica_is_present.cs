// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_CollaborationRoster.when_joining;

public class and_another_session_from_the_same_replica_is_present : Specification
{
    CollaborationRoster _roster = null!;
    Guid _replica;
    IReadOnlyList<Collaborator> _others = null!;

    void Establish()
    {
        _roster = new CollaborationRoster();
        _replica = Guid.NewGuid();
        _roster.Join(new Collaborator("session-a", Guid.NewGuid().ToString(), "Alice", "#fff", 0, 0, string.Empty, string.Empty, false), Guid.NewGuid(), _replica);
    }

    void Because()
    {
        // Sessions on the caller's own replica are already known locally, so they must not be returned again — the hub
        // merges its own local sockets with only the sessions on OTHER replicas to avoid a duplicate in the snapshot.
        _others = _roster.Join(new Collaborator("session-b", Guid.NewGuid().ToString(), "Bob", "#000", 0, 0, string.Empty, string.Empty, false), Guid.NewGuid(), _replica);
    }

    [Fact] void should_not_return_same_replica_sessions() => _others.Count.ShouldEqual(0);
}
