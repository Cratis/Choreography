// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;

namespace Cratis.Orleans.Workflows.Definitions;

/// <summary>
/// The <see cref="IWorkflowDefinitionSource"/> that asks whichever discovered
/// <see cref="IWorkflowDefinitions"/> owns a workflow.
/// </summary>
/// <remarks>
/// A workflow no source owns has no active version, so its admission is rejected with
/// <see cref="WorkflowAdmissionRejection.NoActiveVersion"/> - deliberately loud rather than
/// empty-handed in a way that looks like success. Two sources claiming one workflow is a defect, and
/// says so rather than letting one of them win silently.
/// </remarks>
/// <param name="sources">Every discovered source of workflow definitions - resolved from the admitting scope, so a source may read that scope's event store.</param>
public class CompositeWorkflowDefinitionSource(IInstancesOf<IWorkflowDefinitions> sources) : IWorkflowDefinitionSource
{
    /// <inheritdoc/>
    public Task<WorkflowDefinitionSnapshot?> GetActive(WorkflowKey workflow)
    {
        var owners = sources.Where(source => source.Owns(workflow)).ToList();

        return owners.Count switch
        {
            0 => Task.FromResult<WorkflowDefinitionSnapshot?>(null),
            1 => owners[0].GetActive(workflow),
            _ => throw new MoreThanOneWorkflowDefinitionSource(workflow),
        };
    }
}
