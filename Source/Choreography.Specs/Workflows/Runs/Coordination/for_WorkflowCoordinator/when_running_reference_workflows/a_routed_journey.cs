// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_running_reference_workflows;

public class a_routed_journey : given.reference_capabilities
{
    void Establish()
    {
        Register("route", _ => WorkflowNodeOutcome.Succeeded("plan"));
        Register("plan", _ => WorkflowNodeOutcome.Succeeded("next", "{\"planned\":true}"));
        Admit([Trigger(), Decider("route", "plan", "approver"), Action("plan"), Approval("approver", TimeSpan.Zero)],
            [Edge("trigger", "next", "route"), Edge("route", "plan", "plan"), Edge("route", "approver", "approver")]);
    }

    async Task Because()
    {
        await Drive(Root("route"));
        await Drive(Root("plan"));
    }

    [Fact] void should_complete_the_planned_journey() => State.Status.ShouldEqual(WorkflowRunStatus.Succeeded);
    [Fact] void should_record_the_unused_approval_as_unreachable() => StatusOf(Root("approver")).ShouldEqual(NodeExecutionStatus.Unreachable);
}
