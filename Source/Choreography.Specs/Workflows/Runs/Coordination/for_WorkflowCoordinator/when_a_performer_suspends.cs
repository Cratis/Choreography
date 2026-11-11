// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Waits;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_a_performer_suspends : given.a_coordinator
{
    void Establish() => Admit([Trigger(), Action("ask", WorkflowNodePolicy.Default, "answered", "expired"), Action("done")], [Edge("trigger", "next", "ask"), Edge("ask", "answered", "done")]);

    void Because() => Report(Root("ask"), WorkflowNodeOutcome.Suspended("invocation-1", TimeSpan.FromMinutes(10)));

    [Fact] void should_open_a_completion_wait() => Recorded<WorkflowWaitOpened>().Single().Kind.ShouldEqual(WaitKind.Completion);
    [Fact] void should_key_the_wait_by_the_completion_key() => Recorded<WorkflowWaitOpened>().Single().Correlation.Value.ShouldEqual("invocation-1");
    [Fact] void should_expire_the_wait_after_the_given_time() => Recorded<WorkflowWaitOpened>().Single().Deadline.Value.ShouldEqual(Now + TimeSpan.FromMinutes(10));
    [Fact] void should_make_the_node_wait() => StatusOf(Root("ask")).ShouldEqual(NodeExecutionStatus.Waiting);
    [Fact] void should_not_leave_the_node() => Recorded<WorkflowNodeSucceeded>().ShouldBeEmpty();
    [Fact] void should_not_finish_the_run() => Decision.State.Outcome.ShouldBeNull();
    [Fact] void should_not_retry() => Recorded<WorkflowNodeAttemptFailed>().ShouldBeEmpty();
}
