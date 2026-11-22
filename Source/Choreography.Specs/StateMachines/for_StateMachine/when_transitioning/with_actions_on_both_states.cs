// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.StateMachines.for_StateMachine.when_transitioning;

public class with_actions_on_both_states : given.a_state_machine_with_actions
{
    async Task Because()
    {
        await StateMachine.TransitionTo<StateWithActions>();
        await StateMachine.TransitionTo<OtherStateWithActions>();
    }

    [Fact] void should_execute_entry_then_running_then_exit_then_the_next_states_actions() => _log.ShouldEqual(["A.entry", "A.running", "A.exit", "B.entry", "B.running"]);
}
