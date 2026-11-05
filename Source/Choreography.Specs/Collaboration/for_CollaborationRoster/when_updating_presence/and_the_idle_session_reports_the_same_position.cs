// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_CollaborationRoster.when_updating_presence;

public class and_the_idle_session_reports_the_same_position : Specification
{
    CollaborationRoster _roster = null!;

    void Establish()
    {
        _roster = new();
        _roster.Join(new Collaborator("session", Guid.NewGuid().ToString(), "Alice", "fff", 1, 1, "board", "surface", true), Guid.NewGuid(), Guid.NewGuid());
    }

    void Because() => _roster.Presence("session", 1, 1, "board", "surface");

    [Fact] void should_stay_idle() => _roster.All.Single().IsIdle.ShouldBeTrue();
}
