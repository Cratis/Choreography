// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_getting_the_history_with_a_blocked_top : given.an_engine
{
    IReadOnlyList<OperationHistoryItem> _items;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        await Rename(Bob, "a", "a3");
        _items = await Engine.GetHistory(Scope, Alice);
    }

    [Fact] void should_not_allow_undo() => _items[0].CanUndo.ShouldBeFalse();
    [Fact] void should_say_why() => _items[0].BlockedReason.ShouldEqual("The module was renamed again after this operation");
}
