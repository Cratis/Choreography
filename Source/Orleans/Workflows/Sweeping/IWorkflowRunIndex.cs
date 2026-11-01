// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Sweeping;

/// <summary>
/// Defines the index of the runs of one scope that are not over. Keyed by the scope's value.
/// </summary>
/// <remarks>
/// It exists so the sweep can find a run that has something to do without a read model: a run adds itself when it is
/// admitted and removes itself once it is over and has nothing left to deliver.
/// </remarks>
public interface IWorkflowRunIndex : IGrainWithStringKey
{
    /// <summary>
    /// Adds a run.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunId"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Add(WorkflowRunId run);

    /// <summary>
    /// Removes a run.
    /// </summary>
    /// <param name="run">The <see cref="WorkflowRunId"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(WorkflowRunId run);

    /// <summary>
    /// Gets the runs that are not over.
    /// </summary>
    /// <returns>The runs.</returns>
    Task<IReadOnlyList<WorkflowRunId>> GetActive();
}
