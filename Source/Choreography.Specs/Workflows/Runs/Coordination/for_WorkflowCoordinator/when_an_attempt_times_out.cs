// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_an_attempt_times_out : given.a_coordinator
{
    void Establish() => Admit([Trigger(), Action("a")], [Edge("trigger", "next", "a")]);

    void Because() => Fail(Root("a"), WorkflowFailureKind.TimedOut);

    [Fact] void should_record_the_attempt_as_timed_out() => Recorded<WorkflowNodeAttemptFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.TimedOut);
    [Fact] void should_fail_the_node_as_timed_out() => Recorded<WorkflowNodeFailed>().Single().Kind.ShouldEqual(WorkflowFailureKind.TimedOut);
}
