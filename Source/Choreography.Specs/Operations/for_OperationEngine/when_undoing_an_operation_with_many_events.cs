// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_undoing_an_operation_with_many_events : given.an_engine
{
    OperationOutcome _outcome;
    IReadOnlyList<OperationEntry> _history;

    async Task Because()
    {
        await Engine.Execute(Scope, Alice, new given.ArchiveModules(["a", "b", "c"]));
        _outcome = await Engine.Undo(Scope, Alice);
        _history = await History.GetFor(Scope);
    }

    [Fact] void should_undo_everything_as_one_unit() => Host.Archived.ShouldBeEmpty();
    [Fact] void should_have_recorded_the_original_as_one_operation() => _history[0].EventSequenceNumbers.AsEnumerable().ShouldEqual([1UL, 2UL, 3UL]);
    [Fact] void should_record_a_single_undo_operation() => _history.Count.ShouldEqual(2);
    [Fact] void should_associate_all_undo_events_with_it() => _history[1].EventSequenceNumbers.AsEnumerable().ShouldEqual([4UL, 5UL, 6UL]);
}
