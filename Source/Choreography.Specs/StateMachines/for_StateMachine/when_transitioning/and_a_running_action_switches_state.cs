// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.StateMachines.for_StateMachine.when_transitioning;

public class and_a_running_action_switches_state : given.a_state_machine_with_actions
{
    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates() =>
    [
        new StateWithActions(Log("A.entry"), [.. Log("A.running"), TransitionStateAction.To<OtherStateWithActions>()], Log("A.exit")),
        new OtherStateWithActions(Log("B.entry"), Log("B.running"), Log("B.exit"))
    ];

    async Task Because() => await StateMachine.TransitionTo<StateWithActions>();

    [Fact] void should_perform_the_switch_after_the_state_has_been_entered_and_run() => _log.ShouldEqual(["A.entry", "A.running", "A.exit", "B.entry", "B.running"]);
    [Fact] async Task should_end_up_in_the_target_state() => (await StateMachine.GetCurrentState()).ShouldBeOfExactType<OtherStateWithActions>();
    [Fact] void should_persist_the_target_state() => _stateStorage.State.CurrentState.ShouldEqual(typeof(OtherStateWithActions).FullName);
}
