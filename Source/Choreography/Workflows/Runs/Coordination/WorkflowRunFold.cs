// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// Folds a run's events into its <see cref="WorkflowRunState"/>.
/// </summary>
/// <remarks>
/// The one place a run's state changes. The coordinator applies every event it decides on through here
/// as it plans, and a grain rebuilds a run by replaying its stream through here - so a run resumed
/// after a restart is exactly the run that was interrupted.
/// </remarks>
public static class WorkflowRunFold
{
    /// <summary>Replays a run's history, without the times its events were recorded at.</summary>
    /// <param name="run">The <see cref="WorkflowRunId"/>.</param>
    /// <param name="events">The events, in the order they were appended.</param>
    /// <returns>The <see cref="WorkflowRunState"/>.</returns>
    /// <remarks>Every pending retry replayed this way is due at once.</remarks>
    public static WorkflowRunState Replay(WorkflowRunId run, IEnumerable<object> events) =>
        Replay(run, events.Select(@event => (@event, DateTimeOffset.MinValue)));

    /// <summary>Replays a run's history.</summary>
    /// <param name="run">The <see cref="WorkflowRunId"/>.</param>
    /// <param name="events">The events with the time each was recorded at, in the order they were appended.</param>
    /// <returns>The <see cref="WorkflowRunState"/>.</returns>
    public static WorkflowRunState Replay(WorkflowRunId run, IEnumerable<(object Event, DateTimeOffset At)> events)
    {
        var state = new WorkflowRunState(run);
        foreach (var (@event, at) in events)
        {
            Apply(state, @event, at);
        }

        return state;
    }

    /// <summary>Applies one event.</summary>
    /// <param name="state">The <see cref="WorkflowRunState"/> to change.</param>
    /// <param name="event">The event.</param>
    /// <param name="at">When the event was recorded - a retry is due its node's retry delay after the failed attempt.</param>
    public static void Apply(WorkflowRunState state, object @event, DateTimeOffset at)
    {
        switch (@event)
        {
            case WorkflowRunAdmitted admitted:
                state.Admission = admitted;
                break;
            case WorkflowNodeInputPinned pinned:
                state.Inputs.TryAdd(pinned.Execution, pinned.Input);
                break;
            case WorkflowJobStopped stopped:
                state.PendingJobStops.Remove(stopped.Job);
                break;
            case WorkflowNodeScheduled scheduled:
                Scheduled(state, scheduled);
                break;
            case WorkflowNodeOperationCompleted operation:
                state.Operations[(operation.Execution, operation.Attempt, operation.Operation)] = operation.Output;
                break;
            case WorkflowNodeSucceeded succeeded:
                Succeeded(state, succeeded.Execution, succeeded.Outlet, succeeded.Output).Evidence = [.. succeeded.Evidence];
                break;
            case WorkflowNodeSkipped skipped:
                Ended(state, skipped.Execution, NodeExecutionStatus.Skipped, skipped.Reason);
                break;
            case WorkflowNodeAttemptFailed attemptFailed:
                AttemptFailed(state, attemptFailed, at);
                break;
            case WorkflowNodeFailed failed:
                Failed(state, failed);
                break;
            case WorkflowNodeOutcomeAmbiguous ambiguous:
                Ended(state, ambiguous.Execution, NodeExecutionStatus.ReconciliationRequired, ambiguous.Reason);
                break;
            case WorkflowNodesUnreachable unreachable:
                foreach (var execution in unreachable.Executions)
                {
                    state.ExecutionFor(execution).Status = NodeExecutionStatus.Unreachable;
                }

                break;
            default:
                ApplyBranchesWaitsAndCompletion(state, @event);
                break;
        }
    }

    static void ApplyBranchesWaitsAndCompletion(WorkflowRunState state, object @event)
    {
        switch (@event)
        {
            case WorkflowBranchesFannedOut fannedOut:
                FannedOut(state, fannedOut);
                break;
            case WorkflowBranchesJoined joined:
                Joined(state, joined);
                break;
            case WorkflowWaitOpened opened:
                state.ExecutionFor(opened.Execution).Status = NodeExecutionStatus.Waiting;
                state.OpenWaits[opened.Wait] = new(opened.Wait, opened.Execution, opened.Kind, opened.Deadline);
                break;
            case WorkflowWaitResolved resolved when state.OpenWaits.Remove(resolved.Wait, out var wait):
                Succeeded(state, wait.Execution, resolved.Outlet, resolved.Payload);
                break;
            case WorkflowWaitCancelled cancelled when state.OpenWaits.Remove(cancelled.Wait, out var wait):
                state.ExecutionFor(wait.Execution).Status = NodeExecutionStatus.Cancelled;
                break;
            case WorkflowRunSucceeded:
                state.Outcome = WorkflowRunStatus.Succeeded;
                break;
            case WorkflowRunFailed:
                state.Outcome = WorkflowRunStatus.Failed;
                break;
            case WorkflowRunNeedsReconciliation:
                state.Outcome = WorkflowRunStatus.ReconciliationRequired;
                break;
            case WorkflowRunCancelled:
                Cancelled(state);
                break;
        }
    }

    static void Scheduled(WorkflowRunState state, WorkflowNodeScheduled scheduled)
    {
        // Legacy histories without an explicit pin still recover the input at the original schedule,
        // not from later sibling outputs. New histories always pin before scheduling.
        if (!state.Inputs.ContainsKey(scheduled.Execution))
        {
            state.Inputs[scheduled.Execution] = NodeInputComposer.For(state, scheduled.Execution);
        }

        var execution = state.ExecutionFor(scheduled.Execution);
        execution.Status = NodeExecutionStatus.Scheduled;
        execution.Attempt = scheduled.Attempt;
        execution.Job = scheduled.Job;
        execution.Wave = scheduled.Wave;
        state.LastWave = Math.Max(state.LastWave, scheduled.Wave);
    }

    static NodeExecution Succeeded(WorkflowRunState state, NodeExecutionKey key, OutletName outlet, WorkflowData output)
    {
        var execution = state.ExecutionFor(key);
        execution.Status = NodeExecutionStatus.Succeeded;
        execution.Outlet = outlet;
        execution.Output = output;
        state.Routes.Add(new(key, outlet, key.Branch));
        return execution;
    }

    static void Ended(WorkflowRunState state, NodeExecutionKey key, NodeExecutionStatus status, WorkflowFailureReason reason)
    {
        var execution = state.ExecutionFor(key);
        execution.Status = status;
        execution.Reason = reason;
    }

    static void AttemptFailed(WorkflowRunState state, WorkflowNodeAttemptFailed attemptFailed, DateTimeOffset at)
    {
        var execution = state.ExecutionFor(attemptFailed.Execution);
        execution.FailureKind = attemptFailed.Kind;
        execution.Reason = attemptFailed.Reason;
        if (state.Graph.NodeFor(attemptFailed.Execution.Node) is { } node && RetryPolicy.Retries(state, node, attemptFailed.Kind, attemptFailed.Attempt))
        {
            execution.Status = NodeExecutionStatus.RetryPending;
            execution.NextAttempt = attemptFailed.Attempt.Next;
            execution.NotBefore = at + node.Policy.RetryDelay;
        }
    }

    static void Cancelled(WorkflowRunState state)
    {
        state.IsCancelled = true;
        foreach (var execution in state.Executions.Values.Where(execution => execution.Status == NodeExecutionStatus.Scheduled))
        {
            state.PendingJobStops.Add(execution.Job);
        }
    }

    static void Failed(WorkflowRunState state, WorkflowNodeFailed failed)
    {
        var execution = state.ExecutionFor(failed.Execution);
        execution.Status = NodeExecutionStatus.Failed;
        execution.Attempt = failed.Attempt;
        execution.FailureKind = failed.Kind;
        execution.Reason = failed.Reason;
        execution.Outlet = failed.RoutedTo;
        if (failed.RoutedTo != OutletName.None)
        {
            state.Routes.Add(new(failed.Execution, failed.RoutedTo, failed.Execution.Branch));
        }
        else if (failed.Execution.Branch == WorkflowBranchKey.Root)
        {
            state.UnroutedFailure ??= new($"'{failed.Execution.Node}' failed and nothing handles it: {failed.Reason}");
        }
    }

    static void FannedOut(WorkflowRunState state, WorkflowBranchesFannedOut fannedOut)
    {
        var branches = fannedOut.Branches.ToList();
        var execution = state.ExecutionFor(fannedOut.ForEach);
        execution.Status = NodeExecutionStatus.Succeeded;
        execution.Outlet = WellKnownOutlets.Each;
        state.FanOuts[fannedOut.ForEach] = new(fannedOut.ForEach, branches, false);
        state.Routes.Add(new(fannedOut.ForEach, WellKnownOutlets.Each, fannedOut.ForEach.Branch, ActivatesJoin: true));
        foreach (var branch in branches)
        {
            state.Branches[branch.Key] = new(fannedOut.ForEach, branch.Item);
            state.Routes.Add(new(fannedOut.ForEach, WellKnownOutlets.Each, branch.Key));
        }
    }

    static void Joined(WorkflowRunState state, WorkflowBranchesJoined joined)
    {
        Succeeded(state, joined.Join, joined.Outlet, joined.Results);
        var forEach = ActivationAnalysis.Of(state).ForEachOf(joined.Join);
        if (forEach is not null && state.FanOuts.TryGetValue(forEach, out var fanOut))
        {
            state.FanOuts[forEach] = fanOut with { IsJoined = true };
        }
    }
}
