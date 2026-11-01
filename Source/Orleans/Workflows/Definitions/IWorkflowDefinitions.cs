// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Definitions;

/// <summary>
/// Defines one place active workflow versions come from - a library of authored workflows, or a feature
/// that derives its workflows from its own facts.
/// </summary>
/// <remarks>
/// Implementations are discovered, and <see cref="CompositeWorkflowDefinitionSource"/> asks the one that
/// owns a workflow. Owning is decided from the workflow's key alone, so two sources never compete for one.
/// </remarks>
public interface IWorkflowDefinitions
{
    /// <summary>
    /// Whether this source owns a workflow.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <returns><see langword="true"/> when this source answers for it.</returns>
    bool Owns(WorkflowKey workflow);

    /// <summary>
    /// Gets a workflow's active version.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <returns>The active version, or <see langword="null"/> when it has none.</returns>
    Task<WorkflowDefinitionSnapshot?> GetActive(WorkflowKey workflow);
}
