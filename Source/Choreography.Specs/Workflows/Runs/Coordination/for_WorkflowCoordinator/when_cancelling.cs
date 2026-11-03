// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_cancelling : given.a_coordinator
{
    void Establish() => Admit([Trigger(), Action("a")], [Edge("trigger", "next", "a")]);

    void Because() => Cancel();

    [Fact] void should_record_the_cancellation() => Recorded<WorkflowRunCancelled>().Single().Reason.ShouldEqual(new WorkflowFailureReason("called off"));
    [Fact] void should_stop_the_running_wave() => Decision.ToStop.ShouldContainOnly(CoordinatorDecision.JobFor(Run.Run, 1));
    [Fact] void should_record_only_the_cancellation() => Decision.Events.ShouldContainOnly(Recorded<WorkflowRunCancelled>());
    [Fact] void should_owe_the_stop_until_it_is_delivered() => State.PendingJobStops.ShouldContainOnly(CoordinatorDecision.JobFor(Run.Run, 1));
    [Fact] void should_leave_the_run_cancelled() => State.Status.ShouldEqual(WorkflowRunStatus.Cancelled);
}
