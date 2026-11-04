// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_executing_a_command_without_a_definition : given.an_engine
{
    OperationOutcome _outcome;

    async Task Because() => _outcome = await Engine.Execute(Scope, Alice, "not defined");

    [Fact] void should_be_blocked() => _outcome.Kind.ShouldEqual(OperationOutcomeKind.Blocked);
    [Fact] async Task should_not_record_anything() => (await History.GetFor(Scope)).ShouldBeEmpty();
}
