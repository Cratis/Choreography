// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_suspended_node_is_completed : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("ask", WorkflowNodePolicy.Default, "answered", "expired"), Action("done")], [Edge("trigger", "next", "ask"), Edge("ask", "answered", "done")]);
        Report(Root("ask"), WorkflowNodeOutcome.Suspended("invocation-1", TimeSpan.FromMinutes(10)));
    }

    void Because() => ResolveWaitOf(Root("ask"), "answered", "{\"answer\":\"yes\"}");

    [Fact] void should_resolve_the_wait_through_the_chosen_outlet() => Recorded<WorkflowWaitResolved>().Single().Outlet.Value.ShouldEqual("answered");
    [Fact] void should_carry_the_answer_as_the_output() => State.Executions[Root("ask")].Output.Value.ShouldEqual("{\"answer\":\"yes\"}");
    [Fact] void should_succeed_the_node() => StatusOf(Root("ask")).ShouldEqual(NodeExecutionStatus.Succeeded);
    [Fact] void should_continue_down_the_chosen_outlet() => ScheduledNow.ShouldContainOnly(Root("done"));
}
