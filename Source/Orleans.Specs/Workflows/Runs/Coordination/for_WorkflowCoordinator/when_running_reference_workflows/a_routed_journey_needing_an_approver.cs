// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_running_reference_workflows;

public class a_routed_journey_needing_an_approver : given.reference_capabilities
{
    void Establish()
    {
        Register("route", _ => WorkflowNodeOutcome.Succeeded("approver"));
        Admit([Trigger(), Decider("route", "plan", "approver"), Action("plan"), Approval("approver", TimeSpan.Zero)],
            [Edge("trigger", "next", "route"), Edge("route", "plan", "plan"), Edge("route", "approver", "approver")]);
    }

    Task Because() => Drive(Root("route"));

    [Fact] void should_wait_without_a_worker() => State.Status.ShouldEqual(WorkflowRunStatus.Waiting);
    [Fact] void should_open_the_approver_approval() => Recorded<WorkflowWaitOpened>().Single().Execution.ShouldEqual(Root("approver"));
    [Fact] void should_not_run_the_planning_action() => State.Executions.ContainsKey(Root("plan")).ShouldBeFalse();
}
