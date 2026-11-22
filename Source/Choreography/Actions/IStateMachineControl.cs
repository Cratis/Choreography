// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions;

/// <summary>
/// Defines the control an action has over a state machine, without knowing the type of its stored state.
/// </summary>
public interface IStateMachineControl
{
    /// <summary>
    /// Gets the <see cref="GrainIdentity"/> of the state machine.
    /// </summary>
    GrainIdentity Identity { get; }

    /// <summary>
    /// Check if the state machine can transition from its current state to a state.
    /// </summary>
    /// <param name="stateType">The type of state to check.</param>
    /// <returns>True if it can, false if not.</returns>
    Task<bool> CanTransitionTo(Type stateType);

    /// <summary>
    /// Transition the state machine to a state, if the current state allows it.
    /// </summary>
    /// <param name="stateType">The type of state to transition to.</param>
    /// <returns>True if the transition was admitted, false if the current state refused it.</returns>
    /// <remarks>
    /// A transition requested while the state machine is already transitioning - from an action that runs when a state
    /// is entered or running - is admitted and performed once the transition in progress has finished.
    /// </remarks>
    Task<bool> TransitionTo(Type stateType);
}
