// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_undoing_after_an_unrelated_change : given.an_engine
{
    OperationOutcome _outcome;
    IReadOnlyList<OperationEntry> _history;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        await Rename(Bob, "c", "c2");
        _outcome = await Engine.Undo(Scope, Alice);
        _history = await History.GetFor(Scope);
    }

    [Fact] void should_succeed() => _outcome.Kind.ShouldEqual(OperationOutcomeKind.Succeeded);
    [Fact] void should_restore_the_module() => Host.Names["a"].ShouldEqual("a");
    [Fact] void should_leave_the_other_actors_change() => Host.Names["c"].ShouldEqual("c2");
    [Fact] void should_append_the_undo_as_a_new_operation() => _history.Count.ShouldEqual(3);
    [Fact] void should_mark_the_original_as_undone_without_removing_it() => _history[0].Status.ShouldEqual(OperationStatus.Undone);
    [Fact] void should_keep_the_original_event_references() => _history[0].EventSequenceNumbers.AsEnumerable().ShouldEqual([1UL]);
    [Fact] void should_associate_the_undo_with_its_target() => _history[2].Target.ShouldEqual(_history[0].Id);
    [Fact] void should_give_the_undo_its_own_events() => _history[2].EventSequenceNumbers.AsEnumerable().ShouldEqual([3UL]);
    [Fact] void should_record_the_undo_role() => _history[2].Role.ShouldEqual(OperationRole.Undo);
}
