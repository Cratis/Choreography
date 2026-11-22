// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.StateMachines.for_StateMachine.when_transitioning;

public class and_an_entry_action_fails : given.a_state_machine_with_actions
{
    Exception? _failure;

    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates() =>
    [
        new StateWithActions([new CodeAction((_, _) => Task.FromResult(ActionResult.Failed("nope")))], Log("A.running"))
    ];

    async Task Because() => _failure = await Catch.Exception(StateMachine.TransitionTo<StateWithActions>);

    [Fact] void should_throw_state_action_failed() => _failure.ShouldBeOfExactType<StateActionFailed>();
    [Fact] void should_report_the_phase() => ((StateActionFailed)_failure!).Phase.ShouldEqual(StateActionPhase.Entry);
    [Fact] void should_not_execute_running_actions() => _log.ShouldBeEmpty();
}
