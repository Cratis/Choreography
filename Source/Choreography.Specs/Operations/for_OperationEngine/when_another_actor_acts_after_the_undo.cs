// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_another_actor_acts_after_the_undo : given.an_engine
{
    OperationOutcome _unrelated;
    OperationOutcome _conflicting;
    IReadOnlyList<OperationHistoryItem> _items;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        await Engine.Undo(Scope, Alice);
        await Rename(Bob, "c", "c2");
        _unrelated = await Engine.Redo(Scope, Alice);
        await Engine.Undo(Scope, Alice);
        await Rename(Bob, "a", "a9");
        _items = await Engine.GetHistory(Scope, Alice);
        _conflicting = await Engine.Redo(Scope, Alice);
    }

    [Fact] void should_not_clear_the_redo_stack_for_an_unrelated_change() => _unrelated.Kind.ShouldEqual(OperationOutcomeKind.Succeeded);
    [Fact] void should_make_redo_ineligible_after_a_conflicting_change() => _conflicting.Kind.ShouldEqual(OperationOutcomeKind.Blocked);
    [Fact] void should_say_why() => _conflicting.Reason.ShouldEqual("The module was renamed by someone else since the undo");
    [Fact] void should_not_apply_the_redo() => Host.Names["a"].ShouldEqual("a9");
    [Fact] void should_report_the_blocked_redo_in_the_history() => _items.Single(_ => _.BlockedReason is not null).CanRedo.ShouldBeFalse();
}
