// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Validation;
using Cratis.Choreography.Actions;

namespace Cratis.Choreography.Arc.Actions.for_ExecuteCommandAction.when_executing;

public class a_command_that_is_not_valid : given.a_command_pipeline
{
    ActionResult _result;

    void Establish() => PipelineReturns(new CommandResult { ValidationResults = [ValidationResult.Error("Name is required")] });

    async Task Because() => _result = await new ExecuteCommandAction(_ => new CommandForTesting()).Execute(_context, CancellationToken.None);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_carry_the_validation_message() => _result.Reason.ShouldContain("Name is required");
}
