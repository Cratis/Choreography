// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations.for_OperationEngine;

public class when_the_host_fails_to_execute : given.an_engine
{
    OperationOutcome _outcome;
    OperationOutcome _undo;
    IReadOnlyList<OperationEntry> _history;

    async Task Because()
    {
        Host.Fail = true;
        _outcome = await Rename(Alice, "a", "a2");
        _undo = await Engine.Undo(Scope, Alice);
        _history = await History.GetFor(Scope);
    }

    [Fact] void should_report_failure() => _outcome.Kind.ShouldEqual(OperationOutcomeKind.Failed);
    [Fact] void should_say_why() => _outcome.Reason.ShouldEqual("The host rejected the command");
    [Fact] void should_record_the_failed_operation() => _history[0].Status.ShouldEqual(OperationStatus.Failed);
    [Fact] void should_not_put_it_on_the_undo_stack() => _undo.Kind.ShouldEqual(OperationOutcomeKind.NothingToUndo);
}
