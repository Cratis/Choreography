// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Adapters;

/// <summary>
/// Represents a base for an <see cref="IWorkflowNodePerformer"/> that performs a node by executing an <see cref="IAction"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is how the actions state machines use are plugged into workflows: derive from it, say which capability it
/// performs and which action it executes - a <see cref="CodeAction"/>, a <see cref="CommandAction"/>, a
/// <see cref="CompositeAction"/> of several, or any other <see cref="IAction"/>. The action sees the node's input as its
/// <see cref="ActionContext.Input"/> and the workflow run as its <see cref="ActionContext.Identity"/>.
/// </para>
/// <para>
/// A successful action leaves the node through <see cref="SuccessOutlet"/> with the action's output; a failed one fails the node with
/// the action's reason, and an exception fails the attempt, so the node's retry policy applies as for any performer.
/// </para>
/// </remarks>
/// <param name="services">The <see cref="IServiceProvider"/> the action resolves its dependencies from.</param>
public abstract class ActionNodePerformer(IServiceProvider services) : IWorkflowNodePerformer
{
    /// <inheritdoc/>
    public abstract CapabilityReference Capability { get; }

    /// <summary>
    /// Gets the outlet a successful action leaves the node through. Defaults to <see cref="WellKnownOutlets.Next"/>.
    /// </summary>
    protected virtual OutletName SuccessOutlet => WellKnownOutlets.Next;

    /// <summary>
    /// Gets the <see cref="IAction"/> that performs the node.
    /// </summary>
    protected abstract IAction Action { get; }

    /// <inheritdoc/>
    public async Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken)
    {
        var identity = new GrainIdentity(GrainKind.WorkflowRun, WorkflowRunGrain.GrainTypeName, invocation.Run.ToString());
        var context = new ActionContext(identity, services) { Input = invocation.Input.Value };

        var result = await Action.Execute(context, cancellationToken);
        return result.IsSuccess
            ? WorkflowNodeOutcome.Succeeded(SuccessOutlet, new WorkflowData(result.Output))
            : WorkflowNodeOutcome.Failed(result.Reason);
    }
}
