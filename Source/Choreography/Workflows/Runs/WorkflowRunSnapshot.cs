// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Runs.Coordination;
using Cratis.Choreography.Workflows.Waits;

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// Where a run stands, as its grain knows it from the run's history.
/// </summary>
/// <param name="Run">The <see cref="WorkflowRunId"/>.</param>
/// <param name="IsAdmitted">Whether the run was admitted.</param>
/// <param name="Status">The <see cref="WorkflowRunStatus"/>.</param>
/// <param name="Executions">The <see cref="NodeExecutionSnapshot">executions</see> the history recorded anything for.</param>
/// <param name="OpenWaits">The open <see cref="WorkflowWaitId">waits</see>.</param>
public record WorkflowRunSnapshot(
    WorkflowRunId Run,
    bool IsAdmitted,
    WorkflowRunStatus Status,
    IEnumerable<NodeExecutionSnapshot> Executions,
    IEnumerable<WorkflowWaitId> OpenWaits)
{
    /// <summary>
    /// Creates a snapshot of a run's state.
    /// </summary>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <returns>The <see cref="WorkflowRunSnapshot"/>.</returns>
    public static WorkflowRunSnapshot Of(WorkflowRunState state) => new(
        state.Run,
        state.IsAdmitted,
        state.Status,
        [.. state.Executions.Values.Select(execution => new NodeExecutionSnapshot(execution.Key, execution.Status, execution.Attempt, execution.Outlet)
        {
            Reason = execution.Reason,
            IdempotencyKey = Coordination.WorkflowPlanner.IdempotencyKeyFor(state.Run, execution.Key)
        })],
        [.. state.OpenWaits.Keys]);
}
