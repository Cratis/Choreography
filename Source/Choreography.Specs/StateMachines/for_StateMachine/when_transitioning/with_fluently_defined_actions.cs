// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.StateMachines.for_StateMachine.when_transitioning;

public class with_fluently_defined_actions : given.a_state_machine_with_actions
{
    protected override IEnumerable<IState<StateMachineStateForTesting>> CreateStates() =>
    [
        new FluentState(_log),
        new StateWithActions()
    ];

    async Task Because()
    {
        await StateMachine.TransitionTo<FluentState>();
        await StateMachine.TransitionTo<StateWithActions>();
    }

    [Fact] void should_execute_what_was_defined_for_each_moment() => _log.ShouldEqual(["entry", "running", "exit"]);

    class FluentState(List<string> log) : State<StateMachineStateForTesting>
    {
        public override Task<bool> CanTransitionTo<TState>(StateMachineStateForTesting state) => Task.FromResult(true);

        protected override void DefineActions(StateActionsBuilder actions) => actions
            .OnEntry(entry => entry.Run(_ => log.Add("entry")))
            .WhileRunning(running => running.Run(_ => log.Add("running")))
            .OnExit(exit => exit.Run(_ => log.Add("exit")));
    }
}
