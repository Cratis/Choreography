// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Actions;

namespace Cratis.Choreography.StateMachines;

/// <summary>
/// Represents an implementation of <see cref="IState{TStoredState}"/>.
/// </summary>
/// <remarks>
/// The owning <see cref="StateMachine{TStoredState}"/> sets <see cref="StateMachine"/> on every state deriving from
/// this type when it activates, regardless of which assembly the state is defined in. For testing a state in isolation,
/// use <see cref="StateExtensions.SetStateMachine{TStoredState}(State{TStoredState}, IStateMachine{TStoredState})"/>.
/// </remarks>
/// <typeparam name="TStoredState">Type of state object associated.</typeparam>
public class State<TStoredState> : IState<TStoredState>, IStateActions
{
    StateActionsBuilder? _defined;

    /// <inheritdoc/>
    public IStateMachine<TStoredState> StateMachine { get; internal set; } = default!;

    /// <inheritdoc/>
    IEnumerable<IAction> IStateActions.EntryActions => EntryActions;

    /// <inheritdoc/>
    IEnumerable<IAction> IStateActions.RunningActions => RunningActions;

    /// <inheritdoc/>
    IEnumerable<IAction> IStateActions.ExitActions => ExitActions;

    /// <summary>
    /// Gets the supported state transitions from this state.
    /// </summary>
    protected virtual IImmutableList<Type> AllowedTransitions => ImmutableList<Type>.Empty;

    /// <summary>
    /// Gets the actions to execute as the state machine enters the state, after <see cref="OnEnter"/>.
    /// </summary>
    /// <remarks>
    /// They run when the state machine transitions into the state. They do not run again when the state machine
    /// reactivates in a state it was already in.
    /// </remarks>
    protected virtual IEnumerable<IAction> EntryActions => Defined.EntryActions;

    /// <summary>
    /// Gets the actions to execute once the state machine is in the state - it has entered and the state is persisted.
    /// </summary>
    /// <remarks>
    /// They run after every entry, and when the state machine reactivates in the state. An action that switches state
    /// is performed once the transition in progress has finished.
    /// </remarks>
    protected virtual IEnumerable<IAction> RunningActions => Defined.RunningActions;

    /// <summary>
    /// Gets the actions to execute as the state machine leaves the state, after <see cref="OnLeave"/>.
    /// </summary>
    /// <remarks>
    /// They run when the state machine transitions out of the state, not when the grain deactivates.
    /// </remarks>
    protected virtual IEnumerable<IAction> ExitActions => Defined.ExitActions;

    StateActionsBuilder Defined
    {
        get
        {
            if (_defined is null)
            {
                var builder = new StateActionsBuilder();
                DefineActions(builder);
                _defined = builder;
            }

            return _defined;
        }
    }

    /// <inheritdoc/>
    public virtual Task<bool> CanTransitionTo<TTargetState>(TStoredState state) => Task.FromResult(AllowedTransitions.Contains(typeof(TTargetState)));

    /// <inheritdoc/>
    public virtual Task<TStoredState> OnEnter(TStoredState state) => Task.FromResult(state);

    /// <inheritdoc/>
    public virtual Task<TStoredState> OnLeave(TStoredState state) => Task.FromResult(state);

    /// <summary>
    /// Define the actions of the state fluently - what to execute as the state is entered, while it is running and as it
    /// is left. Overriding EntryActions, RunningActions or ExitActions replaces what is defined here
    /// for that moment.
    /// </summary>
    /// <param name="actions">The <see cref="StateActionsBuilder"/> to define the actions on.</param>
    protected virtual void DefineActions(StateActionsBuilder actions)
    {
    }
}
