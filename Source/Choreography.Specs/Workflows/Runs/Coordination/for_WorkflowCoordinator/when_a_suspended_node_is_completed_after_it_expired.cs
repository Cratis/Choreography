// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_suspended_node_is_completed_after_it_expired : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("ask", WorkflowNodePolicy.Default, "answered", "expired")], [Edge("trigger", "next", "ask")]);
        Report(Root("ask"), WorkflowNodeOutcome.Suspended("invocation-1", TimeSpan.FromMinutes(10)));
        ExpireWaitsAt(Now + TimeSpan.FromMinutes(10));
    }

    void Because() => ResolveWaitOf(Root("ask"), "answered");

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
    [Fact] void should_keep_the_expiry() => State.Executions[Root("ask")].Outlet.Value.ShouldEqual("expired");
}
