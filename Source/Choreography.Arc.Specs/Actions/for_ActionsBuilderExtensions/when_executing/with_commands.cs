// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Actions;

namespace Cratis.Choreography.Arc.Actions.for_ActionsBuilderExtensions.when_executing;

public class with_commands : given.a_command_pipeline
{
    ActionResult _result;

    async Task Because() => _result = await new ActionsBuilder()
        .Execute<CommandForTesting>(command => command.Name = "populated")
        .Execute<CommandForTesting>((command, context) => command.Name = context.Identity.Key)
        .Execute(context => new RecordCommandForTesting("created"))
        .Build()
        .Execute(_context, CancellationToken.None);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_execute_the_populated_command() => ((CommandForTesting)_executed[0]).Name.ShouldEqual("populated");
    [Fact] void should_execute_the_command_populated_from_the_context() => ((CommandForTesting)_executed[1]).Name.ShouldEqual("a-key");
    [Fact] void should_execute_the_created_command() => _executed[2].ShouldEqual(new RecordCommandForTesting("created"));
}
