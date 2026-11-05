// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_CollaborationRoster.when_joining;

public class and_a_collaborator_from_another_replica_is_present : Specification
{
    CollaborationRoster _roster = null!;
    Guid _replicaA;
    Guid _replicaB;
    IReadOnlyList<Collaborator> _othersForReplicaB = null!;

    void Establish()
    {
        _roster = new CollaborationRoster();
        _replicaA = Guid.NewGuid();
        _replicaB = Guid.NewGuid();
        _roster.Join(new Collaborator("session-a", Guid.NewGuid().ToString(), "Alice", "#fff", 0, 0, string.Empty, string.Empty, false), Guid.NewGuid(), _replicaA);
    }

    void Because() => _othersForReplicaB = _roster.Join(new Collaborator("session-b", Guid.NewGuid().ToString(), "Bob", "#000", 0, 0, string.Empty, string.Empty, false), Guid.NewGuid(), _replicaB);

    [Fact] void should_return_exactly_one_other() => _othersForReplicaB.Count.ShouldEqual(1);

    [Fact] void should_show_the_other_replicas_collaborator() => _othersForReplicaB.Any(c => c.SessionId == "session-a").ShouldBeTrue();

    [Fact] void should_not_include_the_joining_session() => _othersForReplicaB.Any(c => c.SessionId == "session-b").ShouldBeFalse();
}
