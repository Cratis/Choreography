// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_suspended_node_is_completed_through_an_undeclared_outlet : given.a_coordinator
{
    void Establish()
    {
        Admit([Trigger(), Action("ask", WorkflowNodePolicy.Default, "answered")], [Edge("trigger", "next", "ask")]);
        Report(Root("ask"), WorkflowNodeOutcome.Suspended("invocation-1"));
    }

    void Because() => ResolveWaitOf(Root("ask"), "somethingElse");

    [Fact] void should_record_nothing() => Decision.Events.ShouldBeEmpty();
    [Fact] void should_keep_the_node_waiting() => StatusOf(Root("ask")).ShouldEqual(NodeExecutionStatus.Waiting);
}
