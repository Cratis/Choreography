// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Runs;
using Cratis.Orleans.Workflows.Triggers;

namespace Cratis.Orleans.Workflows;

/// <summary>
/// Represents an implementation of <see cref="IWorkflows"/>.
/// </summary>
/// <param name="grains">The <see cref="IGrainFactory"/>.</param>
public class WorkflowStarter(IGrainFactory grains) : IWorkflows
{
    /// <inheritdoc/>
    public Task<WorkflowAdmission> Start(WorkflowScope scope, WorkflowId workflow, WorkflowTrigger trigger) =>
        grains.GetGrain<IWorkflow>(new WorkflowKey(scope, workflow).ToString()).Admit(trigger);

    /// <inheritdoc/>
    public IWorkflowRun GetRun(WorkflowScope scope, WorkflowRunId run) =>
        grains.GetGrain<IWorkflowRun>(new WorkflowRunKey(scope, run).ToString());
}
