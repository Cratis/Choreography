// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;
using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Definitions;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Triggers;
using Cratis.Orleans.Workflows.Waits;

namespace Cratis.Orleans.Workflows.Runs.Coordination;

/// <summary>
/// Decides what a run records and does in answer to everything that can happen to it.
/// </summary>
/// <remarks>
/// Pure: every answer is a function of the run's recorded state, the input and the time, and nothing is
/// changed until the caller has recorded the decision's events. Input that arrives for something no
/// longer expecting it - a second report, a superseded attempt, a wait already closed - decides nothing.
/// Outcomes reported after a run was cancelled are still recorded, because they are facts about the
/// outside world, but nothing is planned from them.
/// </remarks>
public static class WorkflowCoordinator
{
    /// <summary>
    /// Admits a run.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunKey"/>.</param>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="definition">The <see cref="WorkflowDefinitionSnapshot"/> to pin.</param>
    /// <param name="trigger">The <see cref="WorkflowTrigger"/>.</param>
    /// <param name="now">The time.</param>
    /// <returns>The <see cref="CoordinatorDecision"/> - empty when the run was already admitted.</returns>
    public static CoordinatorDecision Admit(WorkflowRunKey run, WorkflowRunState state, WorkflowDefinitionSnapshot definition, WorkflowTrigger trigger, DateTimeOffset now) =>
        Decide(run, state, now, work =>
        {
            if (work.State.IsAdmitted)
            {
                return false;
            }

            work.Record(new WorkflowRunAdmitted(definition.Workflow, definition.Area, definition.Version, definition.CatalogVersion, [.. definition.Capabilities], definition.Graph, trigger));
            return true;
        });

    /// <summary>
    /// Records a performer's outcome.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunKey"/>.</param>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/>.</param>
    /// <param name="outcome">The <see cref="WorkflowNodeOutcome"/>.</param>
    /// <param name="now">The time.</param>
    /// <returns>The <see cref="CoordinatorDecision"/>.</returns>
    public static CoordinatorDecision CompleteNode(WorkflowRunKey run, WorkflowRunState state, NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowNodeOutcome outcome, DateTimeOffset now) =>
        Decide(run, state, now, work => OutcomeRecording.Record(work, execution, attempt, outcome));

    /// <summary>
    /// Records that an attempt failed in a way the performer did not report itself - it timed out, or no performer exists.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunKey"/>.</param>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/>.</param>
    /// <param name="kind">The <see cref="WorkflowFailureKind"/>.</param>
    /// <param name="reason">The <see cref="WorkflowFailureReason"/>.</param>
    /// <param name="now">The time.</param>
    /// <returns>The <see cref="CoordinatorDecision"/>.</returns>
    public static CoordinatorDecision FailAttempt(WorkflowRunKey run, WorkflowRunState state, NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowFailureKind kind, WorkflowFailureReason reason, DateTimeOffset now) =>
        Decide(run, state, now, work => OutcomeRecording.RecordFailure(work, execution, attempt, kind, reason));

    /// <summary>
    /// Records that one operation of a staged node completed.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunKey"/>.</param>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/>.</param>
    /// <param name="operation">The <see cref="WorkflowOperationName"/>.</param>
    /// <param name="output">What it produced.</param>
    /// <param name="now">The time.</param>
    /// <returns>The <see cref="CoordinatorDecision"/>.</returns>
    public static CoordinatorDecision CompleteOperation(WorkflowRunKey run, WorkflowRunState state, NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowOperationName operation, WorkflowData output, DateTimeOffset now) =>
        Decide(run, state, now, work => OutcomeRecording.RecordOperation(work, execution, attempt, operation, output));

    /// <summary>
    /// Resolves an open wait.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunKey"/>.</param>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="wait">The <see cref="WorkflowWaitId"/>.</param>
    /// <param name="outlet">The <see cref="OutletName"/>.</param>
    /// <param name="payload">What came with it.</param>
    /// <param name="actor">The <see cref="WorkflowActor"/>.</param>
    /// <param name="now">The time.</param>
    /// <returns>The <see cref="CoordinatorDecision"/>.</returns>
    public static CoordinatorDecision ResolveWait(WorkflowRunKey run, WorkflowRunState state, WorkflowWaitId wait, OutletName outlet, WorkflowData payload, WorkflowActor actor, DateTimeOffset now) =>
        Decide(run, state, now, work => WaitHandling.Resolve(work, wait, outlet, payload, actor));

    /// <summary>
    /// Expires every wait whose deadline has passed.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunKey"/>.</param>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="now">The time.</param>
    /// <returns>The <see cref="CoordinatorDecision"/>.</returns>
    public static CoordinatorDecision ExpireDueWaits(WorkflowRunKey run, WorkflowRunState state, DateTimeOffset now) =>
        Decide(run, state, now, WaitHandling.ExpireDue);

    /// <summary>
    /// Starts every retry whose time has come.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunKey"/>.</param>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="now">The time.</param>
    /// <returns>The <see cref="CoordinatorDecision"/>.</returns>
    public static CoordinatorDecision StartDueRetries(WorkflowRunKey run, WorkflowRunState state, DateTimeOffset now) =>
        Decide(run, state, now, work =>
        {
            var due = work.State.IsOver
                ? []
                : work.State.Executions.Values.Where(execution => execution.Status == NodeExecutionStatus.RetryPending && execution.NotBefore <= now).ToList();
            foreach (var execution in due)
            {
                WorkflowPlanner.Schedule(work, execution.Key, execution.NextAttempt);
            }

            return due.Count > 0;
        });

    /// <summary>
    /// Takes a run forward again from its recorded state - rescheduling scheduled work whose wave job is gone.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunKey"/>.</param>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="runningJobs">The wave jobs that are still running.</param>
    /// <param name="now">The time.</param>
    /// <returns>The <see cref="CoordinatorDecision"/>.</returns>
    public static CoordinatorDecision Resume(WorkflowRunKey run, WorkflowRunState state, IEnumerable<WorkflowJobReference> runningJobs, DateTimeOffset now) =>
        Decide(run, state, now, work =>
        {
            var running = runningJobs.ToHashSet();
            var orphaned = work.State.IsOver
                ? []
                : work.State.Executions.Values.Where(execution => execution.Status == NodeExecutionStatus.Scheduled && !running.Contains(execution.Job)).ToList();
            foreach (var execution in orphaned)
            {
                WorkflowPlanner.Schedule(work, execution.Key, execution.Attempt);
            }

            return true;
        });

    /// <summary>
    /// Cancels a run.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunKey"/>.</param>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="reason">The <see cref="WorkflowFailureReason"/>.</param>
    /// <param name="now">The time.</param>
    /// <returns>The <see cref="CoordinatorDecision"/>.</returns>
    public static CoordinatorDecision Cancel(WorkflowRunKey run, WorkflowRunState state, WorkflowFailureReason reason, DateTimeOffset now) =>
        Decide(run, state, now, work =>
        {
            if (!work.State.IsAdmitted || work.State.IsOver)
            {
                return false;
            }

            // The jobs to stop follow from the cancellation itself - every job holding scheduled work - and
            // stay owed in the run's state until WorkflowJobStopped is recorded for each.
            WaitHandling.CancelAll(work);
            work.Record(new WorkflowRunCancelled(reason));
            foreach (var job in work.State.PendingJobStops)
            {
                work.Stop(job);
            }
            return false;
        });

    static CoordinatorDecision Decide(WorkflowRunKey run, WorkflowRunState state, DateTimeOffset now, Func<CoordinatorWork, bool> handle, bool plan = true)
    {
        var work = new CoordinatorWork(run, state, now);
        if (handle(work) && plan)
        {
            WorkflowPlanner.Plan(work);
        }

        return work.Decide();
    }
}
