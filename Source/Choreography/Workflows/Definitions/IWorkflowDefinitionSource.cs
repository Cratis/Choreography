// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Definitions;

/// <summary>
/// Defines where the engine finds the active version of a workflow.
/// </summary>
public interface IWorkflowDefinitionSource
{
    /// <summary>
    /// Gets the active version of a workflow.
    /// </summary>
    /// <param name="workflow">The <see cref="WorkflowKey"/>.</param>
    /// <returns>The <see cref="WorkflowDefinitionSnapshot"/>, or null when the workflow has no active version.</returns>
    Task<WorkflowDefinitionSnapshot?> GetActive(WorkflowKey workflow);
}
