// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions.for_TransitionStateAction.when_executing;

public class with_a_state_machine_in_the_context : given.an_action_context
{
    IStateMachineControl _machine;
    ActionResult _result;

    void Establish()
    {
        _machine = Substitute.For<IStateMachineControl>();
        _machine.TransitionTo(typeof(string)).Returns(true);
        _context = _context with { StateMachine = _machine };
    }

    async Task Because() => _result = await TransitionStateAction.To<string>().Execute(_context, CancellationToken.None);

    [Fact] void should_transition_the_state_machine() => _machine.Received(1).TransitionTo(typeof(string));
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_say_it_transitioned() => _result.Output.ShouldEqual("""{"transitioned":true}""");
}
