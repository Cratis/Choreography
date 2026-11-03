// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.StateMachines;

public class StateThrowingOnGuard : State<StateMachineStateForTesting>
{
    public bool ShouldThrow { get; set; } = true;

    public override Task<bool> CanTransitionTo<TTargetState>(StateMachineStateForTesting state) =>
        ShouldThrow ? throw new SimulatedStateTransitionError() : Task.FromResult(true);
}
