// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_the_latest_operation_is_blocked : given.an_engine
{
    OperationOutcome _outcome;

    async Task Because()
    {
        await Rename(Alice, "a", "a2");
        await Rename(Alice, "b", "b2");
        await Rename(Bob, "b", "b3");
        _outcome = await Engine.Undo(Scope, Alice);
    }

    [Fact] void should_be_blocked() => _outcome.Kind.ShouldEqual(OperationOutcomeKind.Blocked);
    [Fact] void should_not_skip_to_the_older_operation() => Host.Names["a"].ShouldEqual("a2");
    [Fact] void should_not_execute_any_inverse() => Host.Commands.Count.ShouldEqual(3);
}
