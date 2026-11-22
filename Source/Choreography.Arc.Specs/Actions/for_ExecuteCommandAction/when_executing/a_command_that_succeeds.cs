// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.Arc.Actions.for_ExecuteCommandAction.when_executing;

public class a_command_that_succeeds : given.a_command_pipeline
{
    ActionResult _result;

    async Task Because() => _result = await new ExecuteCommandAction(_ => new CommandForTesting { Name = "it" }).Execute(_context, CancellationToken.None);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_execute_the_command_through_the_pipeline() => ((CommandForTesting)_executed.Single()).Name.ShouldEqual("it");
}
