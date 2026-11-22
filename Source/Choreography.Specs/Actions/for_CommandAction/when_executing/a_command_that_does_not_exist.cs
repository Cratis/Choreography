// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions.for_CommandAction.when_executing;

public class a_command_that_does_not_exist : given.an_action_context
{
    ActionResult _result;

    async Task Because() => _result = await new CommandAction("a-command-that-does-not-exist-anywhere").Execute(_context, CancellationToken.None);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_say_it_could_not_be_started() => _result.Reason.ShouldContain("could not be started");
}
