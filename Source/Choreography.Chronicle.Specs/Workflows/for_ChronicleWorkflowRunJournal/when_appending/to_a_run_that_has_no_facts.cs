// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Journal;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Choreography.Chronicle.Workflows.for_ChronicleWorkflowRunJournal.when_appending;

public class to_a_run_that_has_no_facts : given.a_journal
{
    ConcurrencyScope? _scope;

    void Establish() => Store.EventLog
        .AppendMany(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<Cratis.Execution.CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Cratis.Chronicle.Subject?>())
        .Returns(call =>
        {
            _scope = call.Arg<ConcurrencyScope?>();
            return Task.FromResult(new AppendManyResult { SequenceNumbers = [EventSequenceNumber.First] });
        });

    Task Because() => Journal.Append(Key, WorkflowJournalPosition.BeforeFirst, [new WorkflowRunSucceeded()]);

    [Fact] void should_name_that_nothing_was_before() => _scope!.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
}
