// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions.for_TransitionStateAction.when_executing;

public class with_a_resolver_for_another_state_machine_that_refuses : given.an_action_context
{
    IStateMachineControl _own;
    IStateMachineControl _other;
    ActionResult _result;

    void Establish()
    {
        _own = Substitute.For<IStateMachineControl>();
        _other = Substitute.For<IStateMachineControl>();
        _other.TransitionTo(typeof(string)).Returns(false);
        _context = _context with { StateMachine = _own };
    }

    async Task Because() => _result = await new TransitionStateAction(typeof(string), _ => _other).Execute(_context, CancellationToken.None);

    [Fact] void should_transition_the_resolved_state_machine() => _other.Received(1).TransitionTo(typeof(string));
    [Fact] void should_not_touch_its_own_state_machine() => _own.DidNotReceive().TransitionTo(Arg.Any<Type>());
    [Fact] void should_not_treat_a_refusal_as_a_failure() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_say_it_did_not_transition() => _result.Output.ShouldEqual("""{"transitioned":false}""");
}
