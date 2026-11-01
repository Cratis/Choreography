// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowCoordinator;

public class when_opening_a_timer_wait : given.a_coordinator
{
    void Because() => Admit([Trigger(), Timer("timer", TimeSpan.FromMinutes(5)), Action("next")], [Edge("trigger", "next", "timer"), Edge("timer", "elapsed", "next")]);

    [Fact] void should_record_the_deadline() => Recorded<WorkflowWaitOpened>().Single().Deadline.Value.ShouldEqual(Now + TimeSpan.FromMinutes(5));
    [Fact] void should_not_hold_a_worker() => Decision.ToSchedule.ShouldBeEmpty();
    [Fact] void should_still_be_waiting() => State.Status.ShouldEqual(WorkflowRunStatus.Waiting);
}
