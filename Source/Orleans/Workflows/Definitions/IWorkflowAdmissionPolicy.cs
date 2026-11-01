// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Triggers;

namespace Cratis.Orleans.Workflows.Definitions;

/// <summary>
/// Defines a rule a trigger must pass before a workflow run is admitted. Every implementation is
/// discovered and asked.
/// </summary>
public interface IWorkflowAdmissionPolicy
{
    /// <summary>
    /// Checks whether a trigger may start a run of a workflow.
    /// </summary>
    /// <param name="workflow">The <see cref="WorkflowKey"/>.</param>
    /// <param name="definition">The active <see cref="WorkflowDefinitionSnapshot"/>.</param>
    /// <param name="trigger">The <see cref="WorkflowTrigger"/>.</param>
    /// <returns>Every reason not to admit - empty to admit.</returns>
    Task<IEnumerable<WorkflowAdmissionRejection>> Check(WorkflowKey workflow, WorkflowDefinitionSnapshot definition, WorkflowTrigger trigger);
}
