// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_undoing_a_redone_operation : given.an_engine
{
    OperationOutcome _outcome;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        await Engine.Undo(Scope, Alice);
        await Engine.Redo(Scope, Alice);
        _outcome = await Engine.Undo(Scope, Alice);
    }

    [Fact] void should_succeed() => _outcome.Kind.ShouldEqual(OperationOutcomeKind.Succeeded);
    [Fact] void should_restore_the_module() => Host.Names["a"].ShouldEqual("a");
}
