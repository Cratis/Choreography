// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// Where one node execution of a run stands.
/// </summary>
/// <param name="Execution">The <see cref="NodeExecutionKey"/>.</param>
/// <param name="Status">The <see cref="NodeExecutionStatus"/>.</param>
/// <param name="Attempt">The current <see cref="WorkflowAttempt"/>.</param>
/// <param name="Outlet">The <see cref="OutletName"/> it left through, if it has.</param>
public record NodeExecutionSnapshot(NodeExecutionKey Execution, NodeExecutionStatus Status, WorkflowAttempt Attempt, OutletName Outlet)
{
    /// <summary>
    /// Gets why the execution was skipped, failed or needs reconciliation.
    /// </summary>
    public WorkflowFailureReason Reason { get; init; } = WorkflowFailureReason.None;

    /// <summary>
    /// Gets the identity of the invocation - the key its performer deduplicates on, the same for every attempt. What a person
    /// needs to find out whether an ambiguous effect happened.
    /// </summary>
    public Adapters.WorkflowIdempotencyKey IdempotencyKey { get; init; } = new(string.Empty);
}
