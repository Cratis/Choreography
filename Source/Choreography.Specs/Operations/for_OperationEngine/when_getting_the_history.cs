// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_getting_the_history : given.an_engine
{
    IReadOnlyList<OperationHistoryItem> _items;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        await Rename(Alice, "b", "b2");
        await Rename(Bob, "c", "c2");
        _items = await Engine.GetHistory(Scope, Alice);
    }

    [Fact] void should_only_contain_the_actors_operations() => _items.Count.ShouldEqual(2);
    [Fact] void should_be_in_order() => (_items[0].Position < _items[1].Position).ShouldBeTrue();
    [Fact] void should_expose_event_references() => _items[0].EventSequenceNumbers.AsEnumerable().ShouldEqual([1UL]);
    [Fact] void should_only_flag_the_next_undo_target() => _items.Select(_ => _.CanUndo).ShouldEqual([false, true]);
    [Fact] void should_not_offer_redo() => _items.ShouldEachConformTo(_ => !_.CanRedo);
}
