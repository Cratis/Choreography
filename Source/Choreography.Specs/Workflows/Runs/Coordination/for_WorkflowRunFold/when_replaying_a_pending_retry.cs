// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination.for_WorkflowRunFold;

/// <summary>
/// A retry is not recorded; a grain rebuilding the run finds it pending, due the retry delay after the
/// failed attempt was recorded.
/// </summary>
public class when_replaying_a_pending_retry : for_WorkflowCoordinator.given.a_coordinator
{
    static readonly DateTimeOffset _failedAt = Start + TimeSpan.FromMinutes(3);
    WorkflowRunState _replayed = null!;

    void Establish()
    {
        Admit([Trigger(), Action("a", new WorkflowNodePolicy(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(30), 1))], [Edge("trigger", "next", "a")]);
        Fail(Root("a"));
    }

    void Because() => _replayed = WorkflowRunFold.Replay(Run.Run, History.Select(@event => (@event, @event is WorkflowNodeAttemptFailed ? _failedAt : Start)));

    [Fact] void should_have_the_retry_pending() => _replayed.Executions[Root("a")].Status.ShouldEqual(NodeExecutionStatus.RetryPending);
    [Fact] void should_make_the_next_attempt() => _replayed.Executions[Root("a")].NextAttempt.ShouldEqual(new WorkflowAttempt(2));
    [Fact] void should_be_due_the_delay_after_the_failure_was_recorded() => _replayed.Executions[Root("a")].NotBefore.ShouldEqual(_failedAt + TimeSpan.FromSeconds(30));
}
