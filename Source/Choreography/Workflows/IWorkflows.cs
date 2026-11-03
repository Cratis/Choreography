// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Triggers;

namespace Cratis.Choreography.Workflows;

/// <summary>
/// Defines the entry point for starting workflows and reaching their runs from application code.
/// </summary>
/// <remarks>
/// It is a thin facade over the <see cref="IWorkflow"/> and <see cref="IWorkflowRun"/> grains: starting a workflow
/// by hand is just admitting a <see cref="WorkflowTriggerKind.Manual"/> trigger. The other triggers - an event, a state machine
/// entering a state, a schedule - are provided by the Chronicle adapter and admit through the same grains.
/// </remarks>
public interface IWorkflows
{
    /// <summary>
    /// Admits a trigger to a workflow. The same workflow and <see cref="WorkflowTrigger.Deduplication"/> key are always the
    /// same run, so a trigger delivered twice finds the run the first delivery started.
    /// </summary>
    /// <param name="scope">The <see cref="WorkflowScope"/> the workflow belongs to.</param>
    /// <param name="workflow">The <see cref="WorkflowId"/>.</param>
    /// <param name="trigger">The <see cref="WorkflowTrigger"/>.</param>
    /// <returns>The <see cref="WorkflowAdmission"/>.</returns>
    Task<WorkflowAdmission> Start(WorkflowScope scope, WorkflowId workflow, WorkflowTrigger trigger);

    /// <summary>
    /// Gets the grain of a run.
    /// </summary>
    /// <param name="scope">The <see cref="WorkflowScope"/> the run belongs to.</param>
    /// <param name="run">The <see cref="WorkflowRunId"/>.</param>
    /// <returns>The <see cref="IWorkflowRun"/>.</returns>
    IWorkflowRun GetRun(WorkflowScope scope, WorkflowRunId run);
}
