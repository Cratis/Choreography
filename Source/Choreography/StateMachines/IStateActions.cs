// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.StateMachines;

/// <summary>
/// Defines a state that has <see cref="IAction"/>s to execute as the state machine enters, runs in and leaves it.
/// </summary>
/// <remarks>
/// <see cref="State{TStoredState}"/> implements this. A state machine executes the actions of a state that implements it.
/// </remarks>
public interface IStateActions
{
    /// <summary>
    /// Gets the actions to execute as the state machine enters the state, after OnEnter.
    /// </summary>
    IEnumerable<IAction> EntryActions { get; }

    /// <summary>
    /// Gets the actions to execute once the state machine is in the state - it has entered and the state is persisted.
    /// </summary>
    IEnumerable<IAction> RunningActions { get; }

    /// <summary>
    /// Gets the actions to execute as the state machine leaves the state, after OnLeave.
    /// </summary>
    IEnumerable<IAction> ExitActions { get; }
}
