// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.StateMachines;

/// <summary>
/// Represents a fluent builder of the actions of a state, for each <see cref="StateActionPhase"/>.
/// </summary>
public class StateActionsBuilder
{
    readonly ActionsBuilder _entry = new();
    readonly ActionsBuilder _running = new();
    readonly ActionsBuilder _exit = new();

    /// <summary>
    /// Gets the actions to execute as the state machine enters the state.
    /// </summary>
    public IReadOnlyList<IAction> EntryActions => _entry.Actions;

    /// <summary>
    /// Gets the actions to execute once the state machine is in the state.
    /// </summary>
    public IReadOnlyList<IAction> RunningActions => _running.Actions;

    /// <summary>
    /// Gets the actions to execute as the state machine leaves the state.
    /// </summary>
    public IReadOnlyList<IAction> ExitActions => _exit.Actions;

    /// <summary>
    /// Add actions to execute as the state machine enters the state, after OnEnter.
    /// </summary>
    /// <param name="define">Defines the actions.</param>
    /// <returns>The builder for continuation.</returns>
    public StateActionsBuilder OnEntry(Action<ActionsBuilder> define)
    {
        define(_entry);
        return this;
    }

    /// <summary>
    /// Add actions to execute once the state machine is in the state.
    /// </summary>
    /// <param name="define">Defines the actions.</param>
    /// <returns>The builder for continuation.</returns>
    public StateActionsBuilder WhileRunning(Action<ActionsBuilder> define)
    {
        define(_running);
        return this;
    }

    /// <summary>
    /// Add actions to execute as the state machine leaves the state, after OnLeave.
    /// </summary>
    /// <param name="define">Defines the actions.</param>
    /// <returns>The builder for continuation.</returns>
    public StateActionsBuilder OnExit(Action<ActionsBuilder> define)
    {
        define(_exit);
        return this;
    }
}
