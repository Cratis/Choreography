// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Orleans.StateMachines;

namespace Cratis.Orleans.Chronicle.Workflows.for_WorkflowStateExtensions.given;

public class PlainMachineForTesting : Grain, IPlainMachineForTesting, IStateMachine<string>
{
    public Task<IState<string>> GetCurrentState() => throw new NotSupportedException();

    public Task<IImmutableList<IState<string>>> GetStates() => throw new NotSupportedException();

    public Task<bool> CanTransitionTo<TState>()
        where TState : IState<string> => throw new NotSupportedException();

    public Task TransitionTo<TState>()
        where TState : IState<string> => throw new NotSupportedException();
}

