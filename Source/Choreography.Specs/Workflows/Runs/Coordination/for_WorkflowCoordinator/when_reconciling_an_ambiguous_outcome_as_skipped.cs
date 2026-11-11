// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_reconciling_an_ambiguous_outcome_as_skipped : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("a"), Action("b")], [Edge("trigger", "next", "a"), Edge("a", "next", "b")]);
        Report(Root("a"), WorkflowNodeOutcome.Ambiguous("unknown"));
    }

    void Because() => Reconcile(Root("a"), WorkflowNodeOutcome.Skipped("nothing to do"));

    [Fact] void should_skip_the_node() => StatusOf(Root("a")).ShouldEqual(NodeExecutionStatus.Skipped);
    [Fact] void should_not_leave_the_run_waiting_for_reconciliation() => Recorded<WorkflowRunNeedsReconciliation>().ShouldBeEmpty();
}
