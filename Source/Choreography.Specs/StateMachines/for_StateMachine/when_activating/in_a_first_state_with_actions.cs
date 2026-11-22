// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.StateMachines.for_StateMachine.when_activating;

public class in_a_first_state_with_actions : given.a_state_machine_with_actions
{
    protected override Type? InitialState => typeof(StateWithActions);

    async Task Because() => await StateMachine.OnActivateAsync(CancellationToken.None);

    [Fact] void should_execute_the_entry_actions_as_the_machine_enters_its_first_state() => _log.Take(2).ShouldEqual(["A.entry", "A.running"]);
    [Fact] void should_identify_the_state_machine() => StateMachine.Identity.Kind.ShouldEqual(Actions.GrainKind.StateMachine);
}
