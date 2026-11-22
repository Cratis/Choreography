// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.Chronicle.Actions.for_AppendEventAction.when_executing;

public class for_a_state_machine_that_is_not_event_sourced : given.an_append_event_action
{
    ActionResult _result;

    async Task Because() => _result = await new AppendEventAction(_ => new SomethingHappened("it")).Execute(
        new(new(GrainKind.StateMachine, "machine", "a-key-without-an-event-store"), _services),
        CancellationToken.None);

    [Fact] void should_fail_asking_for_a_target() => _result.Reason.ShouldContain("supply a target");
}
