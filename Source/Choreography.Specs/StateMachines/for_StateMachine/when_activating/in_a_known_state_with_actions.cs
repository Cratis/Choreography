// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.StateMachines.for_StateMachine.when_activating;

public class in_a_known_state_with_actions : given.a_state_machine_with_actions
{
    void Establish()
    {
        // We get the state machine so that it calls the OnActivateAsync method before we set the state to test for
        _ = StateMachine;
        _stateStorage.State = new StateMachineStateForTesting { CurrentState = typeof(StateWithActions).FullName };
        _log.Clear();
    }

    async Task Because() => await StateMachine.OnActivateAsync(CancellationToken.None);

    [Fact] void should_only_execute_the_running_actions_since_the_state_was_already_entered() => _log.ShouldEqual(["A.running"]);
}
