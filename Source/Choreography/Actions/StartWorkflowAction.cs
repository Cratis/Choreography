// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Triggers;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Actions;

/// <summary>
/// Represents an <see cref="IAction"/> that starts a workflow.
/// </summary>
/// <remarks>
/// The action succeeds when the workflow admits the trigger and fails with the rejections when it does not. The output
/// holds the id of the run. Starting a workflow is idempotent for the trigger's deduplication key.
/// </remarks>
/// <param name="workflow">The workflow to start.</param>
/// <param name="trigger">Creates the <see cref="WorkflowTrigger"/> from the <see cref="ActionContext"/>.</param>
/// <param name="scope">Optional <see cref="WorkflowScope"/> to start it in - defaults to <see cref="WorkflowScope.Default"/>.</param>
public class StartWorkflowAction(WorkflowId workflow, Func<ActionContext, WorkflowTrigger> trigger, WorkflowScope? scope = default) : IAction
{
    /// <inheritdoc/>
    public async Task<ActionResult> Execute(ActionContext context, CancellationToken cancellationToken)
    {
        var admission = await context.Services.GetRequiredService<IWorkflows>().Start(scope ?? WorkflowScope.Default, workflow, trigger(context));
        return admission.Admitted
            ? ActionResult.Succeeded(JsonSerializer.Serialize(new Dictionary<string, string> { ["run"] = admission.Run.Value }))
            : ActionResult.Failed($"Workflow '{workflow.Value}' did not admit the trigger: {string.Join("; ", admission.Rejections.Select(rejection => rejection.Value))}");
    }
}
