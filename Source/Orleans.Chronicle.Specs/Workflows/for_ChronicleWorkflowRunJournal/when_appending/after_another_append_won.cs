// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Orleans.Workflows.Journal;
using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Chronicle.Workflows.for_ChronicleWorkflowRunJournal.when_appending;

public class after_another_append_won : given.a_journal
{
    WorkflowJournalAppendResult _result = null!;

    void Establish() => Store.EventLog
        .AppendMany(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<Cratis.Execution.CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Cratis.Chronicle.Subject?>())
        .Returns(Task.FromResult(new AppendManyResult { ConcurrencyViolations = [new ConcurrencyViolation(new EventSourceId("run-1"), new EventSequenceNumber(10), new EventSequenceNumber(12))] }));

    async Task Because() => _result = await Journal.Append(Key, new WorkflowJournalPosition(10), [new WorkflowRunSucceeded()]);

    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_a_conflict() => _result.HasConflict.ShouldBeTrue();
    [Fact] void should_keep_the_expected_tail() => _result.Tail.ShouldEqual(new WorkflowJournalPosition(10));
}
