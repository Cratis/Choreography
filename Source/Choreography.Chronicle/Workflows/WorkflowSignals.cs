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
/// Represents an implementation of <see cref="IWorkflowSignals"/> that finds waits in the <see cref="OpenWorkflowWait"/> read model.
/// </summary>
/// <param name="eventStores">The <see cref="IStateMachineEventStores"/> event stores are resolved through.</param>
/// <param name="workflows">The <see cref="IWorkflows"/> the runs are reached through.</param>
/// <param name="options">The <see cref="EventSourcedWorkflowsOptions"/>.</param>
public class WorkflowSignals(IStateMachineEventStores eventStores, IWorkflows workflows, IOptions<EventSourcedWorkflowsOptions> options) : IWorkflowSignals
{
    /// <inheritdoc/>
    public async Task<int> Signal(WorkflowScope scope, WorkflowCorrelationKey correlation, WorkflowContractReference contract, WorkflowData payload, WorkflowActor actor)
    {
        var eventStore = await eventStores.Get(options.Value.EventStore, options.Value.NamespaceFor(scope));
        var waits = (await eventStore.ReadModels.GetInstances<OpenWorkflowWait>())
            .Where(wait => wait.Kind == WaitKind.Signal && wait.Correlation == correlation && (wait.Signal == contract || wait.Signal == WorkflowContractReference.Any))
            .ToList();
        foreach (var wait in waits)
        {
            await workflows.GetRun(scope, wait.Run).ResolveWait(wait.Id, WellKnownOutlets.Resumed, payload, actor);
        }

        return waits.Count;
    }
}
