// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Choreography.Actions;

/// <summary>
/// Represents an <see cref="IAction"/> that switches a state machine to another state.
/// </summary>
/// <remarks>
/// By default it switches the state machine that executes the action. Supply a resolver to switch another state machine.
/// A state machine that refuses the transition is not a failure; the output says whether it transitioned.
/// </remarks>
/// <param name="targetState">The type of state to transition to.</param>
/// <param name="machine">Optional resolver for the state machine to switch, instead of the one executing the action.</param>
public class TransitionStateAction(Type targetState, Func<ActionContext, IStateMachineControl?>? machine = default) : IAction
{
    /// <summary>
    /// Create a <see cref="TransitionStateAction"/> for a state type.
    /// </summary>
    /// <typeparam name="TState">The type of state to transition to.</typeparam>
    /// <returns>The <see cref="TransitionStateAction"/>.</returns>
    public static TransitionStateAction To<TState>() => new(typeof(TState));

    /// <inheritdoc/>
    public async Task<ActionResult> Execute(ActionContext context, CancellationToken cancellationToken)
    {
        var control = machine?.Invoke(context) ?? context.StateMachine;
        if (control is null)
        {
            return ActionResult.Failed($"There is no state machine to transition to '{targetState.Name}'");
        }

        var transitioned = await control.TransitionTo(targetState);
        return ActionResult.Succeeded(JsonSerializer.Serialize(new Dictionary<string, bool> { ["transitioned"] = transitioned }));
    }
}
