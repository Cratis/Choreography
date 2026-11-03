// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_redoing_with_nothing_undone : given.an_engine
{
    OperationOutcome _outcome;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        _outcome = await Engine.Redo(Scope, Alice);
    }

    [Fact] void should_report_nothing_to_redo() => _outcome.Kind.ShouldEqual(OperationOutcomeKind.NothingToRedo);
}
