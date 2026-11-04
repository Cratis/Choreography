// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_redoing_after_an_undo : given.an_engine
{
    OperationOutcome _outcome;
    IReadOnlyList<OperationEntry> _history;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        await Engine.Undo(Scope, Alice);
        _outcome = await Engine.Redo(Scope, Alice);
        _history = await History.GetFor(Scope);
    }

    [Fact] void should_succeed() => _outcome.Kind.ShouldEqual(OperationOutcomeKind.Succeeded);
    [Fact] void should_reissue_the_original_intent() => Host.Commands[^1].ShouldEqual(new given.RenameModule("a", "a2"));
    [Fact] void should_apply_it_again() => Host.Names["a"].ShouldEqual("a2");
    [Fact] void should_record_the_redo_as_a_new_operation() => _history.Count.ShouldEqual(3);
    [Fact] void should_target_the_undone_operation() => _history[2].Target.ShouldEqual(_history[0].Id);
    [Fact] void should_have_the_redo_role() => _history[2].Role.ShouldEqual(OperationRole.Redo);
    [Fact] void should_mark_the_original_as_redone() => _history[0].Status.ShouldEqual(OperationStatus.Redone);
}
