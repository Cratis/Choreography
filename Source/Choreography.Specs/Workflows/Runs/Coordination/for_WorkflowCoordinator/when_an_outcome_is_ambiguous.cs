// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_an_outcome_is_ambiguous : given.a_coordinator
{
    void Establish() => Admit([Trigger(), Action("a", new WorkflowNodePolicy(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30), 1))], [Edge("trigger", "next", "a")]);

    void Because() => Report(Root("a"), WorkflowNodeOutcome.Ambiguous("the provider timed out after accepting"));

    [Fact] void should_record_the_ambiguity() => Recorded<WorkflowNodeOutcomeAmbiguous>().Single().Execution.ShouldEqual(Root("a"));
    [Fact] void should_not_retry() => Recorded<WorkflowNodeAttemptFailed>().ShouldBeEmpty();
    [Fact] void should_require_reconciliation_of_the_node() => StatusOf(Root("a")).ShouldEqual(NodeExecutionStatus.ReconciliationRequired);
    [Fact] void should_finish_the_run_as_needing_reconciliation() => Recorded<WorkflowRunNeedsReconciliation>().ShouldNotBeEmpty();
}
