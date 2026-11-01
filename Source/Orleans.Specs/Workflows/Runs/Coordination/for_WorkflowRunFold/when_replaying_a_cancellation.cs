// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.Coordination.for_WorkflowRunFold;

/// <summary>
/// The stops a cancellation owes follow from the history itself, and a delivered one is no longer owed.
/// </summary>
public class when_replaying_a_cancellation : for_WorkflowCoordinator.given.a_coordinator
{
    WorkflowRunState _cancelled = null!;
    WorkflowRunState _delivered = null!;

    void Establish()
    {
        Admit([Trigger(), Action("a")], [Edge("trigger", "next", "a")]);
        Cancel();
    }

    void Because()
    {
        _cancelled = WorkflowRunFold.Replay(Run.Run, History);
        _delivered = WorkflowRunFold.Replay(Run.Run, [.. History, new WorkflowJobStopped(CoordinatorDecision.JobFor(Run.Run, 1))]);
    }

    [Fact] void should_owe_a_stop_to_the_job_with_scheduled_work() => _cancelled.PendingJobStops.ShouldContainOnly(CoordinatorDecision.JobFor(Run.Run, 1));
    [Fact] void should_owe_nothing_once_the_stop_is_delivered() => _delivered.PendingJobStops.ShouldBeEmpty();
}
