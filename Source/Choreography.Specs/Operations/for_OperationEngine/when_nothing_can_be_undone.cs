// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_nothing_can_be_undone : given.an_engine
{
    OperationOutcome _outcome;

    async Task Because()
    {
        await Rename(Bob, "a", "a2");
        _outcome = await Engine.Undo(Scope, Alice);
    }

    [Fact] void should_report_nothing_to_undo() => _outcome.Kind.ShouldEqual(OperationOutcomeKind.NothingToUndo);
    [Fact] void should_not_touch_the_other_actors_operation() => Host.Names["a"].ShouldEqual("a2");
}
