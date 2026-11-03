// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;

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
        if (!IsScheduled(work.State, execution, attempt))
        {
            return false;
        }

        var node = work.State.Graph.NodeFor(execution.Node)!;
        switch (outcome.Status)
        {
            case WorkflowNodeOutcomeStatus.Succeeded when !node.Declares(outcome.Outlet):
                Fail(work, execution, attempt, WorkflowFailureKind.InvalidOutlet, $"'{node.Id}' answered with '{outcome.Outlet}', which it does not declare");
                break;
            case WorkflowNodeOutcomeStatus.Succeeded when outcome.Output.ExceedsLimit || !outcome.Output.IsValidJson:
                Fail(work, execution, attempt, WorkflowFailureKind.InvalidOutput, $"The output of '{node.Id}' is not JSON of at most {WorkflowData.MaxBytes} bytes");
                break;
            case WorkflowNodeOutcomeStatus.Succeeded:
                work.Record(new WorkflowNodeSucceeded(execution, attempt, outcome.Outlet, outcome.Output, [.. outcome.Evidence]));
                break;
            case WorkflowNodeOutcomeStatus.Skipped:
                work.Record(new WorkflowNodeSkipped(execution, outcome.Reason));
                break;
            case WorkflowNodeOutcomeStatus.Ambiguous:
                work.Record(new WorkflowNodeOutcomeAmbiguous(execution, attempt, outcome.Reason));
                break;
            default:
                Fail(work, execution, attempt, WorkflowFailureKind.Error, outcome.Reason);
                break;
        }

        return true;
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
        if (!IsScheduled(work.State, execution, attempt))
        {
            return false;
        }

        Fail(work, execution, attempt, kind, reason);
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
        if (!IsScheduled(work.State, execution, attempt) || work.State.Operations.ContainsKey((execution, attempt, operation)))
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
        Fail(work, execution, attempt, WorkflowFailureKind.InvalidOutput, reason);

    static bool IsScheduled(WorkflowRunState state, NodeExecutionKey execution, WorkflowAttempt attempt) =>
        state.Executions.TryGetValue(execution, out var recorded)
            && recorded.Status == NodeExecutionStatus.Scheduled
            && recorded.Attempt == attempt;

    static void Fail(CoordinatorWork work, NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowFailureKind kind, WorkflowFailureReason reason)
    {
        var state = work.State;
        var node = state.Graph.NodeFor(execution.Node)!;
        var retries = RetryPolicy.Retries(state, node, kind, attempt);
        work.Record(new WorkflowNodeAttemptFailed(execution, attempt, kind, reason));

        // A retry is not a fact until it starts: the failed attempt makes it pending, due the node's
        // retry delay from now, and the run's sweep starts it through StartDueRetries.
        if (retries)
        {
            return;
        }

        var routedTo = state.IsCancelled ? Graphs.OutletName.None : FailureRouting.For(state.Graph, node.Id, kind);
        work.Record(new WorkflowNodeFailed(execution, attempt, kind, reason, routedTo));
    }
}
