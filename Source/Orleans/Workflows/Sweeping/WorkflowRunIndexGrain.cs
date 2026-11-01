// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Sweeping;

/// <summary>
/// Represents an implementation of <see cref="IWorkflowRunIndex"/>.
/// </summary>
/// <param name="state">The persistent state.</param>
[GrainType("cratis-orleans-workflow-run-index")]
public class WorkflowRunIndexGrain(
    [PersistentState(nameof(WorkflowRunIndexState), WellKnownGrainStorageProviders.Workflows)] IPersistentState<WorkflowRunIndexState> state) : Grain, IWorkflowRunIndex
{
    /// <inheritdoc/>
    public async Task Add(WorkflowRunId run)
    {
        if (state.State.Runs.Add(run.Value))
        {
            await state.WriteStateAsync();
        }
    }

    /// <inheritdoc/>
    public async Task Remove(WorkflowRunId run)
    {
        if (state.State.Runs.Remove(run.Value))
        {
            await state.WriteStateAsync();
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<WorkflowRunId>> GetActive() =>
        Task.FromResult<IReadOnlyList<WorkflowRunId>>([.. state.State.Runs.Select(run => new WorkflowRunId(run))]);
}
