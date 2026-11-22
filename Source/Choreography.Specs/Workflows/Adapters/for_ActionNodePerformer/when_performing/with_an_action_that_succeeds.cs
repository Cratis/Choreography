// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Adapters.for_ActionNodePerformer.when_performing;

public class with_an_action_that_succeeds : given.an_action_performer
{
    WorkflowNodeOutcome _outcome;

    protected override ActionResult ActionResultToReturn => ActionResult.Succeeded("""{"out":2}""");

    async Task Because() => _outcome = await _performer.Perform(_invocation, CancellationToken.None);

    [Fact] void should_succeed_through_the_next_outlet() => _outcome.Outlet.ShouldEqual(WellKnownOutlets.Next);
    [Fact] void should_succeed() => _outcome.Status.ShouldEqual(WorkflowNodeOutcomeStatus.Succeeded);
    [Fact] void should_output_what_the_action_output() => _outcome.Output.Value.ShouldEqual("""{"out":2}""");
    [Fact] void should_give_the_action_the_node_input() => _contextSeen!.Input.ShouldEqual("""{"in":1}""");
    [Fact] void should_identify_the_workflow_run_to_the_action() => _contextSeen!.Identity.ShouldEqual(new GrainIdentity(GrainKind.WorkflowRun, "cratis-orleans-workflow-run", Run.ToString()));
}
