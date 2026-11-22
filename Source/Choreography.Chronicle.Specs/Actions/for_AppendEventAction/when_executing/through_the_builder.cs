// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.Chronicle.Actions.for_AppendEventAction.when_executing;

public class through_the_builder : given.an_append_event_action
{
    public class PopulatedEvent
    {
        public string What { get; set; } = string.Empty;
    }

    ActionResult _result;

    void Establish() => AppendSucceeds();

    async Task Because() => _result = await new ActionsBuilder()
        .Append<PopulatedEvent>((@event, context) => @event.What = context.Identity.Key)
        .Build()
        .Execute(new(new(GrainKind.StateMachine, "machine", "store/namespace/source-1"), _services), CancellationToken.None);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_the_populated_event() => ((PopulatedEvent)_appended!).What.ShouldEqual("store/namespace/source-1");
}
