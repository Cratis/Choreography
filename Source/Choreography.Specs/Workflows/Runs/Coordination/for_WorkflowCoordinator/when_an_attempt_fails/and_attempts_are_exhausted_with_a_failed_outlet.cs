// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator.when_an_attempt_fails;

public class and_attempts_are_exhausted_with_a_failed_outlet : given.a_coordinator
{
    void Establish() => Admit(
        [Trigger(), Action("a", null, "next", "failed"), Action("handle")],
        [Edge("trigger", "next", "a"), Edge("a", "failed", "handle")]);

    void Because() => Fail(Root("a"));

    [Fact] void should_fail_the_node_through_failed() => Recorded<WorkflowNodeFailed>().Single().RoutedTo.ShouldEqual(WellKnownOutlets.Failed);
    [Fact] void should_not_leave_a_retry_pending() => StatusOf(Root("a")).ShouldEqual(NodeExecutionStatus.Failed);
    [Fact] void should_schedule_the_handler() => ScheduledNow.ShouldContainOnly(Root("handle"));
}
