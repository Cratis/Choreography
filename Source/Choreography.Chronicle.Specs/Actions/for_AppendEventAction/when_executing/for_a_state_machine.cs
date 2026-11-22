// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.Chronicle.Actions.for_AppendEventAction.when_executing;

public class for_a_state_machine : given.an_append_event_action
{
    ActionResult _result;

    void Establish() => AppendSucceeds();

    async Task Because() => _result = await new AppendEventAction(_ => new SomethingHappened("it")).Execute(
        new(new(GrainKind.StateMachine, "machine", "store/namespace/source-1"), _services),
        CancellationToken.None);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_use_the_event_store_of_the_state_machine() => _requestedStore.Value.ShouldEqual("store");
    [Fact] void should_use_the_namespace_of_the_state_machine() => _requestedNamespace.Value.ShouldEqual("namespace");
    [Fact] void should_append_to_the_event_source_of_the_state_machine() => _appendedTo.Value.ShouldEqual("source-1");
    [Fact] void should_append_the_event() => _appended.ShouldEqual(new SomethingHappened("it"));
}
