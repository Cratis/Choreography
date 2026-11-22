// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.Chronicle.Actions.for_AppendEventAction.when_executing;

public class for_a_workflow_that_has_no_target : given.an_append_event_action
{
    ActionResult _result;

    async Task Because() => _result = await new AppendEventAction(_ => new SomethingHappened("it")).Execute(
        new(new(GrainKind.Workflow, "workflow", "tenant/workflow-1"), _services),
        CancellationToken.None);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_ask_for_an_event_store() => _eventStores.DidNotReceive().Get(Arg.Any<Cratis.Chronicle.EventStoreName>(), Arg.Any<Cratis.Chronicle.EventStoreNamespaceName>());
}
