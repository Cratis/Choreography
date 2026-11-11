// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_reconciling_an_ambiguous_outcome_as_failed : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("a"), Action("b")], [Edge("trigger", "next", "a"), Edge("a", "next", "b")]);
        Report(Root("a"), WorkflowNodeOutcome.Ambiguous("unknown"));
    }

    void Because() => Reconcile(Root("a"), WorkflowNodeOutcome.Failed("the message was never sent"));

    [Fact] void should_fail_the_node() => StatusOf(Root("a")).ShouldEqual(NodeExecutionStatus.Failed);
    [Fact] void should_not_retry_it() => Recorded<WorkflowNodeAttemptFailed>().ShouldBeEmpty();
    [Fact] void should_fail_the_run_when_nothing_handles_it() => Recorded<WorkflowRunFailed>().ShouldNotBeEmpty();
}
