// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions.for_CommandAction.when_executing;

public class a_command_that_exits_with_an_error : given.an_action_context
{
    ActionResult _result;

    async Task Because() => _result = await new CommandAction("dotnet", ["this-is-not-a-dotnet-command"]).Execute(_context, CancellationToken.None);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_exit_code() => _result.Reason.ShouldContain("exited with code");
}
