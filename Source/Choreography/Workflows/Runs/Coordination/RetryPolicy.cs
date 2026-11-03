// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// Decides whether a failed attempt is tried again - the one rule both the coordinator and the fold apply,
/// so a retry that is pending is never recorded as a fact of its own.
/// </summary>
public static class RetryPolicy
{
    /// <summary>
    /// Gets whether a failed attempt is retried.
    /// </summary>
    /// <param name="state">The <see cref="WorkflowRunState"/> before the failure.</param>
    /// <param name="node">The <see cref="WorkflowNode"/> that failed.</param>
    /// <param name="kind">How it failed.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/> that failed.</param>
    /// <returns>True if another attempt follows.</returns>
    public static bool Retries(WorkflowRunState state, WorkflowNode node, WorkflowFailureKind kind, WorkflowAttempt attempt) =>
        kind is WorkflowFailureKind.Error or WorkflowFailureKind.TimedOut
        && !state.IsCancelled
        && attempt.Value < node.Policy.MaxAttempts;
}
