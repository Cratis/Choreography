// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Triggers;

namespace Cratis.Choreography.Workflows.Definitions;

/// <summary>
/// Refuses to run a graph the engine cannot run - see <see cref="WorkflowGraphStructure"/>.
/// </summary>
public class StructuralAdmissionPolicy : IWorkflowAdmissionPolicy
{
    /// <inheritdoc/>
    public Task<IEnumerable<WorkflowAdmissionRejection>> Check(WorkflowKey workflow, WorkflowDefinitionSnapshot definition, WorkflowTrigger trigger) =>
        Task.FromResult<IEnumerable<WorkflowAdmissionRejection>>(
            [.. WorkflowGraphStructure.Check(definition.Graph).Select(violation => new WorkflowAdmissionRejection(violation.Description))]);
}
