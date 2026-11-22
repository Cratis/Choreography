// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;
using Cratis.Choreography.Chronicle.StateMachines;

namespace Cratis.Choreography.Chronicle.Actions.for_AppendEventAction.when_executing;

public class with_an_explicit_target : given.an_append_event_action
{
    ActionResult _result;

    void Establish() => AppendSucceeds();

    async Task Because() => _result = await new AppendEventAction(
        _ => new SomethingHappened("it"),
        _ => new StateMachineKey("elsewhere", "other", new("source-2"))).Execute(
        new(new(GrainKind.Workflow, "workflow", "not-an-event-sourced-key"), _services),
        CancellationToken.None);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_the_event_store_of_the_target() => _requestedStore.Value.ShouldEqual("elsewhere");
    [Fact] void should_append_to_the_event_source_of_the_target() => _appendedTo.Value.ShouldEqual("source-2");
}
