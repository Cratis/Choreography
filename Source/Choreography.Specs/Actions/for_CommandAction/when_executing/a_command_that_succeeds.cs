// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Actions.for_CommandAction.when_executing;

public class a_command_that_succeeds : given.an_action_context
{
    ActionResult _result;

    async Task Because() => _result = await new CommandAction("dotnet", ["--version"]).Execute(_context, CancellationToken.None);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_output_the_exit_code() => _result.Output.ShouldContain("\"exitCode\":0");
    [Fact] void should_output_the_standard_output() => _result.Output.ShouldContain("standardOutput");
}
