// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_reconciling_an_ambiguous_outcome_as_succeeded : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("a"), Action("b")], [Edge("trigger", "next", "a"), Edge("a", "next", "b")]);
        Report(Root("a"), WorkflowNodeOutcome.Ambiguous("the provider timed out after accepting"));
    }

    void Because() => Reconcile(Root("a"), WorkflowNodeOutcome.Succeeded("next", "{\"messageId\":\"m-1\"}"));

    [Fact] void should_have_stopped_the_run_before() => History.OfType<WorkflowRunNeedsReconciliation>().ShouldNotBeEmpty();
    [Fact] void should_record_who_reconciled_it() => Recorded<WorkflowNodeReconciled>().Single().Actor.Value.ShouldEqual("operator");
    [Fact] void should_record_the_attempt_that_was_ambiguous() => Recorded<WorkflowNodeReconciled>().Single().Attempt.ShouldEqual(Recorded<WorkflowNodeSucceeded>().Single().Attempt);
    [Fact] void should_succeed_the_node_with_the_chosen_output() => State.Executions[Root("a")].Output.Value.ShouldEqual("{\"messageId\":\"m-1\"}");
    [Fact] void should_continue_the_run() => ScheduledNow.ShouldContainOnly(Root("b"));
    [Fact] void should_not_leave_the_run_stopped() => State.Outcome.ShouldBeNull();
}
