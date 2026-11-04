// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_undoing_an_operation_that_has_no_undo : given.an_engine
{
    OperationOutcome _outcome;

    async Task Because()
    {
        await Engine.Execute(Scope, Alice, new given.Untouchable("a"));
        _outcome = await Engine.Undo(Scope, Alice);
    }

    [Fact] void should_be_blocked() => _outcome.Kind.ShouldEqual(OperationOutcomeKind.Blocked);
    [Fact] void should_say_it_cannot_be_undone() => _outcome.Reason.ShouldContain("does not define how to undo");
}
