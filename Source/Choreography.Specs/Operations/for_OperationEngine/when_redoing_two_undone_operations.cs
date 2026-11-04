// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_redoing_two_undone_operations : given.an_engine
{
    IReadOnlyList<OperationEntry> _history;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        await Rename(Alice, "b", "b2");
        await Engine.Undo(Scope, Alice);
        await Engine.Undo(Scope, Alice);
        await Engine.Redo(Scope, Alice);
        _history = await History.GetFor(Scope);
    }

    [Fact] void should_redo_the_most_recently_undone_first() => (Host.Names["a"] + Host.Names["b"]).ShouldEqual("a2b");
    [Fact] void should_leave_the_other_undone() => _history[1].Status.ShouldEqual(OperationStatus.Undone);
}
