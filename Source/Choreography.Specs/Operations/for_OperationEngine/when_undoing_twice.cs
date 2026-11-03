// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_undoing_twice : given.an_engine
{
    IReadOnlyList<OperationEntry> _history;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        await Rename(Alice, "b", "b2");
        await Engine.Undo(Scope, Alice);
        await Engine.Undo(Scope, Alice);
        _history = await History.GetFor(Scope);
    }

    [Fact] void should_undo_the_latest_first() => _history[1].Status.ShouldEqual(OperationStatus.Undone);
    [Fact] void should_then_undo_the_older_one() => _history[0].Status.ShouldEqual(OperationStatus.Undone);
    [Fact] void should_restore_both() => (Host.Names["a"] + Host.Names["b"]).ShouldEqual("ab");
}
