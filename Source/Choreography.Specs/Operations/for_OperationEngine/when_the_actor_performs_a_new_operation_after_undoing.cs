// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_the_actor_performs_a_new_operation_after_undoing : given.an_engine
{
    OperationOutcome _redo;
    IReadOnlyList<OperationEntry> _history;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        await Engine.Undo(Scope, Alice);
        await Rename(Alice, "b", "b2");
        _redo = await Engine.Redo(Scope, Alice);
        _history = await History.GetFor(Scope);
    }

    [Fact] void should_have_nothing_to_redo() => _redo.Kind.ShouldEqual(OperationOutcomeKind.NothingToRedo);
    [Fact] void should_discard_the_redo_candidate() => _history[0].Status.ShouldEqual(OperationStatus.Discarded);
    [Fact] void should_not_reapply_the_old_operation() => Host.Names["a"].ShouldEqual("a");
}
