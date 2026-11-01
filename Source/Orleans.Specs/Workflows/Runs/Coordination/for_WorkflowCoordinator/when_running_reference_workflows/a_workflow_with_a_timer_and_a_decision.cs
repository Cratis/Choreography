// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_running_reference_workflows;

public class a_workflow_with_a_timer_and_a_decision : given.reference_capabilities
{
    void Establish()
    {
        Register("decide", _ => WorkflowNodeOutcome.Succeeded("send"));
        Register("notify", _ => WorkflowNodeOutcome.Succeeded("next", "{\"notified\":true}"));
        Admit(
            [Trigger(), Timer("timer", TimeSpan.FromMinutes(5)), Decider("decide", "send"), Action("notify")],
            [Edge("trigger", "next", "timer"), Edge("timer", "elapsed", "decide"), Edge("decide", "send", "notify")]);
    }

    async Task Because()
    {
        ExpireWaitsAt(Now + TimeSpan.FromMinutes(5));
        await Drive(Root("decide"));
        await Drive(Root("notify"));
    }

    [Fact] void should_complete_without_external_dependencies() => State.Status.ShouldEqual(WorkflowRunStatus.Succeeded);
    [Fact] void should_record_the_notification_output() => State.Executions[Root("notify")].Output.Value.ShouldEqual("{\"notified\":true}");
}
