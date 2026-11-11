// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_suspended_node_expires : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("ask", WorkflowNodePolicy.Default, "answered", "expired"), Action("giveUp")], [Edge("trigger", "next", "ask"), Edge("ask", "expired", "giveUp")]);
        Report(Root("ask"), WorkflowNodeOutcome.Suspended("invocation-1", TimeSpan.FromMinutes(10)));
    }

    void Because() => ExpireWaitsAt(Now + TimeSpan.FromMinutes(10));

    [Fact] void should_resolve_the_wait_through_expired() => Recorded<WorkflowWaitResolved>().Single().Outlet.Value.ShouldEqual("expired");
    [Fact] void should_schedule_the_expiry_path() => ScheduledNow.ShouldContainOnly(Root("giveUp"));
}
