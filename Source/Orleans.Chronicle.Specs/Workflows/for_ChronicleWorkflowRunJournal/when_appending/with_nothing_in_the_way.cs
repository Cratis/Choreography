// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Orleans.Workflows.Journal;
using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Chronicle.Workflows.for_ChronicleWorkflowRunJournal.when_appending;

public class with_nothing_in_the_way : given.a_journal
{
    WorkflowJournalAppendResult _result = null!;
    ConcurrencyScope? _scope;
    IEnumerable<object> _appended = [];

    void Establish() => Store.EventLog
        .AppendMany(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<Cratis.Execution.CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Cratis.Chronicle.Subject?>())
        .Returns(call =>
        {
            _appended = call.Arg<IEnumerable<object>>();
            _scope = call.Arg<ConcurrencyScope?>();
            return Task.FromResult(new AppendManyResult { SequenceNumbers = [new EventSequenceNumber(11), new EventSequenceNumber(12)] });
        });

    async Task Because() => _result = await Journal.Append(Key, new WorkflowJournalPosition(10), [new WorkflowRunSucceeded(), new WorkflowJobStopped("job-1")]);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_end_at_the_last_appended_sequence_number() => _result.Tail.ShouldEqual(new WorkflowJournalPosition(12));
    [Fact] void should_append_every_fact() => _appended.Count().ShouldEqual(2);
    [Fact] void should_name_the_tail_it_decided_from() => _scope!.SequenceNumber.ShouldEqual(new EventSequenceNumber(10));
    [Fact] void should_append_to_the_run() => Store.EventLog.Received(1).AppendMany(Arg.Is<EventSourceId>(id => id.Value == "run-1"), Arg.Any<IEnumerable<object>>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<Cratis.Execution.CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Cratis.Chronicle.Subject?>());
}
