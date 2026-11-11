// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.StateMachines;
using Cratis.Choreography.Chronicle.Workflows.Listing;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Waits;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows;

/// <summary>
/// Represents an implementation of <see cref="IWorkflowCompletions"/> that finds waits in the <see cref="OpenWorkflowWait"/> read model.
/// </summary>
/// <param name="eventStores">The <see cref="IStateMachineEventStores"/> event stores are resolved through.</param>
/// <param name="workflows">The <see cref="IWorkflows"/> the runs are reached through.</param>
/// <param name="options">The <see cref="EventSourcedWorkflowsOptions"/>.</param>
public class WorkflowCompletions(IStateMachineEventStores eventStores, IWorkflows workflows, IOptions<EventSourcedWorkflowsOptions> options) : IWorkflowCompletions
{
    /// <inheritdoc/>
    public async Task<int> Complete(WorkflowScope scope, WorkflowCompletionKey key, OutletName outlet, WorkflowData payload, WorkflowActor actor)
    {
        var eventStore = await eventStores.Get(options.Value.EventStore, options.Value.NamespaceFor(scope));
        var correlation = new WorkflowCorrelationKey(key.Value);
        var waits = (await eventStore.ReadModels.GetInstances<OpenWorkflowWait>())
            .Where(wait => wait.Kind == WaitKind.Completion && wait.Correlation == correlation)
            .ToList();
        foreach (var wait in waits)
        {
            await workflows.GetRun(scope, wait.Run).ResolveWait(wait.Id, outlet, payload, actor);
        }

        return waits.Count;
    }
}
