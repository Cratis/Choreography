// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Scopes;
using Cratis.Choreography.Workflows.Triggers;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Workflows;

/// <summary>
/// Admits triggers against the active stored definition, never against caller-supplied ownership or graphs.
/// </summary>
/// <param name="scopeFactory">Resolves definition storage and admission policies in the scope of the workflow.</param>
/// <param name="scopes">The <see cref="IWorkflowExecutionScopes"/> that establish the context work for a scope runs in.</param>
[GrainType("cratis-orleans-workflow")]
public class WorkflowGrain(IServiceScopeFactory scopeFactory, IWorkflowExecutionScopes scopes) : Grain, IWorkflow
{
    /// <inheritdoc/>
    public async Task<WorkflowAdmission> Admit(WorkflowTrigger trigger)
    {
        var key = WorkflowKey.Parse(this.GetPrimaryKeyString());
        var run = WorkflowRunId.For(key.Workflow, trigger.Deduplication);
        using var scopeContext = scopes.Enter(key.Scope);
        await using var scope = scopeFactory.CreateAsyncScope();
        var definition = await scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionSource>().GetActive(key);
        if (definition is null)
        {
            return WorkflowAdmission.Rejected(run, [WorkflowAdmissionRejection.NoActiveVersion]);
        }

        var rejections = new List<WorkflowAdmissionRejection>();
        if (definition.Workflow != key.Workflow || definition.Subject != trigger.Subject.Kind)
        {
            rejections.Add(new("The active workflow or subject does not match the trigger"));
        }

        if (!trigger.Payload.IsValidJson || trigger.Payload.ExceedsLimit)
        {
            rejections.Add(new("The trigger payload must be JSON of at most 32 KiB"));
        }

        foreach (var policy in scope.ServiceProvider.GetRequiredService<IInstancesOf<IWorkflowAdmissionPolicy>>())
        {
            rejections.AddRange(await policy.Check(key, definition, trigger));
        }

        return rejections.Count > 0
            ? WorkflowAdmission.Rejected(run, rejections)
            : await GrainFactory.GetGrain<IWorkflowRun>(new WorkflowRunKey(key.Scope, run).ToString()).Admit(definition, trigger);
    }
}
