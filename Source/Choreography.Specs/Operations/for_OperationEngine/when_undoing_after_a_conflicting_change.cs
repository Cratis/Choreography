// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_undoing_after_a_conflicting_change : given.an_engine
{
    OperationOutcome _outcome;
    int _entriesBefore;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        await Rename(Bob, "a", "a3");
        _entriesBefore = (await History.GetFor(Scope)).Count;
        _outcome = await Engine.Undo(Scope, Alice);
    }

    [Fact] void should_be_blocked() => _outcome.Kind.ShouldEqual(OperationOutcomeKind.Blocked);
    [Fact] void should_say_why() => _outcome.Reason.ShouldEqual("The module was renamed again after this operation");
    [Fact] void should_not_change_current_state() => Host.Names["a"].ShouldEqual("a3");
    [Fact] async Task should_not_record_anything() => (await History.GetFor(Scope)).Count.ShouldEqual(_entriesBefore);
}
