// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;
using Cratis.Choreography.Chronicle.StateMachines;
using Cratis.Choreography.Chronicle.Workflows;
using Cratis.Choreography.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Actions;

/// <summary>
/// Represents an <see cref="IAction"/> that appends an event to a Chronicle event log.
/// </summary>
/// <remarks>
/// <para>
/// Unless told where to append, the event goes where the grain executing the action lives: for an event-sourced state
/// machine its own event store, namespace and event source id; for a workflow run the workflow's event store and
/// namespace, and the run id as the event source id. Supply a <paramref name="target"/> to append anywhere else.
/// </para>
/// <para>
/// The action fails when Chronicle does not accept the event - a concurrency or constraint violation, or an error.
/// </para>
/// </remarks>
/// <param name="event">Creates the event to append from the <see cref="ActionContext"/>.</param>
/// <param name="target">Optional <see cref="StateMachineKey"/> - event store, namespace and event source id - to append to.</param>
public class AppendEventAction(Func<ActionContext, object> @event, Func<ActionContext, StateMachineKey>? target = default) : IAction
{
    /// <inheritdoc/>
    public async Task<ActionResult> Execute(ActionContext context, CancellationToken cancellationToken)
    {
        var key = target?.Invoke(context) ?? TargetFor(context);
        if (key is null)
        {
            return ActionResult.Failed($"There is no event store to append to for {context.Identity}; supply a target");
        }

        var eventStore = await context.Services.GetRequiredService<IStateMachineEventStores>().Get(key.EventStore, key.Namespace);
        var result = await eventStore.EventLog.Append(key.EventSourceId, @event(context));
        return result.IsSuccess
            ? ActionResult.Succeeded()
            : ActionResult.Failed(string.Join(
                "; ",
                result.Errors.Select(error => error.ToString())
                    .Concat(result.ConstraintViolations.Select(violation => violation.Message.Value))));
    }

    static StateMachineKey? TargetFor(ActionContext context)
    {
        switch (context.Identity.Kind)
        {
            case GrainKind.StateMachine:
                try
                {
                    return StateMachineKey.Parse(context.Identity.Key);
                }
                catch (InvalidStateMachineKey)
                {
                    return null;
                }

            case GrainKind.WorkflowRun:
                var options = context.Services.GetService<IOptions<EventSourcedWorkflowsOptions>>()?.Value;
                if (options is null)
                {
                    return null;
                }

                var run = WorkflowRunKey.Parse(context.Identity.Key);
                return new(options.EventStore, options.NamespaceFor(run.Scope), new(run.Run.Value));

            default:
                return null;
        }
    }
}
