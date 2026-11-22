// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.Workflows.Adapters.for_ActionNodePerformer.when_performing;

public class with_an_action_that_fails : given.an_action_performer
{
    WorkflowNodeOutcome _outcome;

    protected override ActionResult ActionResultToReturn => ActionResult.Failed("broken");

    async Task Because() => _outcome = await _performer.Perform(_invocation, CancellationToken.None);

    [Fact] void should_fail_the_node() => _outcome.Status.ShouldEqual(WorkflowNodeOutcomeStatus.Failed);
    [Fact] void should_carry_the_reason() => _outcome.Reason.Value.ShouldEqual("broken");
}
