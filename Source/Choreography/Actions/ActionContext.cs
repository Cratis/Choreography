// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions;

/// <summary>
/// Represents what an <see cref="IAction"/> executes within.
/// </summary>
/// <param name="Identity">The <see cref="GrainIdentity"/> of the state machine, workflow or workflow run executing the action.</param>
/// <param name="Services">The <see cref="IServiceProvider"/> the action resolves its dependencies from.</param>
public record ActionContext(GrainIdentity Identity, IServiceProvider Services)
{
    /// <summary>
    /// Gets the input to the action, as JSON. For a workflow node it is the node's input.
    /// </summary>
    public string Input { get; init; } = ActionResult.NoOutput;

    /// <summary>
    /// Gets the control over the state machine executing the action. Only set for an action a state machine executes.
    /// </summary>
    public IStateMachineControl? StateMachine { get; init; }

    /// <summary>
    /// Gets the stored state of the state machine executing the action. Only set for an action a state machine executes.
    /// </summary>
    public object? State { get; init; }
}
