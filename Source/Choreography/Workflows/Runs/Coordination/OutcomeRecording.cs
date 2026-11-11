// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// Records what a performer reported about an attempt.
/// </summary>
/// <remarks>
/// Only an attempt that is scheduled can report: a second report, or a report for an attempt that has
/// been superseded, records nothing. An outlet the node does not declare, or output that is not JSON
/// or is larger than <see cref="WorkflowData.MaxBytes"/>, fails the attempt. An error or a timeout is
/// retried while attempts remain - the next attempt is recorded only when it is scheduled; anything else fails the node at once. An ambiguous outcome is never
/// retried - nobody knows whether the effect happened, so a person has to find out.
/// </remarks>
public static class OutcomeRecording
{
    /// <summary>
    /// Records an outcome.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/> it is for.</param>
    /// <param name="outcome">The <see cref="WorkflowNodeOutcome"/>.</param>
    /// <returns>True if it was recorded.</returns>
    public static bool Record(CoordinatorWork work, NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowNodeOutcome outcome)
    {
        var target = new NodeAttempt(execution, attempt);
        if (!IsScheduled(work.State, target))
        {
            return false;
        }

        var node = work.State.Graph.NodeFor(execution.Node)!;
        switch (outcome.Status)
        {
            case WorkflowNodeOutcomeStatus.Succeeded:
                RecordSucceeded(work, target, node, outcome);
                break;
            case WorkflowNodeOutcomeStatus.Skipped:
                work.Record(new WorkflowNodeSkipped(execution, outcome.Reason));
                break;
            case WorkflowNodeOutcomeStatus.Ambiguous:
                work.Record(new WorkflowNodeOutcomeAmbiguous(execution, attempt, outcome.Reason));
                break;
            case WorkflowNodeOutcomeStatus.Suspended:
                if (string.IsNullOrEmpty(outcome.CompletionKey.Value))
                {
                    Fail(work, target, WorkflowFailureKind.InvalidOutput, $"'{node.Id}' suspended without a completion key");
                }
                else
                {
                    WaitHandling.OpenForCompletion(work, execution, outcome.CompletionKey, outcome.ExpiresAfter);
                }

                break;
            default:
                Fail(work, target, WorkflowFailureKind.Error, outcome.Reason);
                break;
        }

        return true;
    }

    /// <summary>
    /// Records the outcome a person chose for a node execution whose outcome was ambiguous, taking the run forward again.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="outcome">The chosen <see cref="WorkflowNodeOutcome"/> - succeeded, skipped or failed.</param>
    /// <param name="actor">The <see cref="WorkflowActor"/> who chose it.</param>
    /// <returns>True if it was recorded.</returns>
    /// <remarks>
    /// Only an execution in the root branch that is waiting for reconciliation of a run that has not been cancelled can be reconciled. A chosen success
    /// must leave through an outlet the node declares with bounded JSON output. The run's nodes recorded as unreachable because of the stop become
    /// reachable again, and the chosen outcome is routed like any other.
    /// </remarks>
    public static bool Reconcile(CoordinatorWork work, NodeExecutionKey execution, WorkflowNodeOutcome outcome, WorkflowActor actor)
    {
        var state = work.State;
        if (state.IsCancelled
            || execution.Branch != WorkflowBranchKey.Root
            || !state.Executions.TryGetValue(execution, out var recorded)
            || recorded.Status != NodeExecutionStatus.ReconciliationRequired
            || state.Graph.NodeFor(execution.Node) is not { } node)
        {
            return false;
        }

        var attempt = recorded.Attempt;
        switch (outcome.Status)
        {
            case WorkflowNodeOutcomeStatus.Succeeded when node.Declares(outcome.Outlet) && !outcome.Output.ExceedsLimit && outcome.Output.IsValidJson:
                work.Record(new WorkflowNodeReconciled(execution, attempt, actor));
                work.Record(new WorkflowNodeSucceeded(execution, attempt, outcome.Outlet, outcome.Output, [.. outcome.Evidence]));
                return true;
            case WorkflowNodeOutcomeStatus.Skipped:
                work.Record(new WorkflowNodeReconciled(execution, attempt, actor));
                work.Record(new WorkflowNodeSkipped(execution, outcome.Reason));
                return true;
            case WorkflowNodeOutcomeStatus.Failed:
                work.Record(new WorkflowNodeReconciled(execution, attempt, actor));
                work.Record(new WorkflowNodeFailed(execution, attempt, WorkflowFailureKind.Error, outcome.Reason, FailureRouting.For(state.Graph, node.Id, WorkflowFailureKind.Error)));
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Records that an attempt failed.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/> that failed.</param>
    /// <param name="kind">The <see cref="WorkflowFailureKind"/>.</param>
    /// <param name="reason">The <see cref="WorkflowFailureReason"/>.</param>
    /// <returns>True if it was recorded.</returns>
    public static bool RecordFailure(CoordinatorWork work, NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowFailureKind kind, WorkflowFailureReason reason)
    {
        var target = new NodeAttempt(execution, attempt);
        if (!IsScheduled(work.State, target))
        {
            return false;
        }

        Fail(work, target, kind, reason);
        return true;
    }

    /// <summary>
    /// Records that one operation of a staged node completed.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/>.</param>
    /// <param name="operation">The <see cref="WorkflowOperationName"/>.</param>
    /// <param name="output">What it produced.</param>
    /// <returns>True if it was recorded.</returns>
    public static bool RecordOperation(CoordinatorWork work, NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowOperationName operation, WorkflowData output)
    {
        if (!IsScheduled(work.State, new NodeAttempt(execution, attempt)) || work.State.Operations.ContainsKey((execution, attempt, operation)))
        {
            return false;
        }

        if (output.ExceedsLimit || !output.IsValidJson)
        {
            RejectData(work, execution, attempt, "The operation output is not bounded valid JSON");
            return true;
        }

        work.Record(new WorkflowNodeOperationCompleted(execution, attempt, operation, output));
        return true;
    }

    /// <summary>
    /// Rejects unbounded or malformed engine-generated data without recording it as successful data.
    /// </summary>
    /// <param name="work">The decision being built.</param>
    /// <param name="execution">The execution.</param>
    /// <param name="attempt">The attempt.</param>
    /// <param name="reason">The rejection reason.</param>
    public static void RejectData(CoordinatorWork work, NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowFailureReason reason) =>
        Fail(work, new NodeAttempt(execution, attempt), WorkflowFailureKind.InvalidOutput, reason);

    static bool IsScheduled(WorkflowRunState state, NodeAttempt target) =>
        state.Executions.TryGetValue(target.Execution, out var recorded)
            && recorded.Status == NodeExecutionStatus.Scheduled
            && recorded.Attempt == target.Attempt;

    static void RecordSucceeded(CoordinatorWork work, NodeAttempt target, WorkflowNode node, WorkflowNodeOutcome outcome)
    {
        if (!node.Declares(outcome.Outlet))
        {
            Fail(work, target, WorkflowFailureKind.InvalidOutlet, $"'{node.Id}' answered with '{outcome.Outlet}', which it does not declare");
            return;
        }

        if (outcome.Output.ExceedsLimit || !outcome.Output.IsValidJson)
        {
            Fail(work, target, WorkflowFailureKind.InvalidOutput, $"The output of '{node.Id}' is not JSON of at most {WorkflowData.MaxBytes} bytes");
            return;
        }

        work.Record(new WorkflowNodeSucceeded(target.Execution, target.Attempt, outcome.Outlet, outcome.Output, [.. outcome.Evidence]));
    }

    static void Fail(CoordinatorWork work, NodeAttempt target, WorkflowFailureKind kind, WorkflowFailureReason reason)
    {
        var state = work.State;
        var node = state.Graph.NodeFor(target.Execution.Node)!;
        var retries = RetryPolicy.Retries(state, node, kind, target.Attempt);
        work.Record(new WorkflowNodeAttemptFailed(target.Execution, target.Attempt, kind, reason));

        // A retry is not a fact until it starts: the failed attempt makes it pending, due the node's
        // retry delay from now, and the run's sweep starts it through StartDueRetries.
        if (retries)
        {
            return;
        }

        var routedTo = state.IsCancelled ? Graphs.OutletName.None : FailureRouting.For(state.Graph, node.Id, kind);
        work.Record(new WorkflowNodeFailed(target.Execution, target.Attempt, kind, reason, routedTo));
    }
}
