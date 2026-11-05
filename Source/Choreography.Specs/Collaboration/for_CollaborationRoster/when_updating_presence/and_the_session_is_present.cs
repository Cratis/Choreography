// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Collaboration.for_CollaborationRoster.when_updating_presence;

public class and_the_session_is_present : Specification
{
    CollaborationRoster _roster = null!;
    Collaborator _updated = null!;

    void Establish()
    {
        _roster = new CollaborationRoster();
        _roster.Join(new Collaborator("session-a", Guid.NewGuid().ToString(), "Alice", "#fff", 0, 0, string.Empty, string.Empty, false), Guid.NewGuid(), Guid.NewGuid());
    }

    void Because()
    {
        _roster.Presence("session-a", 120, 340, "event-model", "model-1");
        _updated = _roster.All.Single(c => c.SessionId == "session-a");
    }

    [Fact] void should_update_the_x_position() => _updated.X.ShouldEqual(120d);

    [Fact] void should_update_the_y_position() => _updated.Y.ShouldEqual(340d);

    [Fact] void should_update_the_area() => _updated.Area.ShouldEqual("event-model");

    [Fact] void should_update_the_event_model() => _updated.SurfaceId.ShouldEqual("model-1");
}
