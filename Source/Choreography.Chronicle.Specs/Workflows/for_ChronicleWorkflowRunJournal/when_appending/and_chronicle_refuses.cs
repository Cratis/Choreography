// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Journal;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Choreography.Chronicle.Workflows.for_ChronicleWorkflowRunJournal.when_appending;

public class and_chronicle_refuses : given.a_journal
{
    WorkflowJournalAppendResult _result = null!;

    void Establish() => Store.EventLog
        .AppendMany(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<object>>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<Cratis.Execution.CorrelationId?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<ConcurrencyScope?>(), Arg.Any<DateTimeOffset?>(), Arg.Any<Cratis.Chronicle.Subject?>())
        .Returns(Task.FromResult(AppendManyResult.Failed(Cratis.Execution.CorrelationId.New(), [new AppendError("the event could not be appended")])));

    async Task Because() => _result = await Journal.Append(Key, new WorkflowJournalPosition(10), [new WorkflowRunSucceeded()]);

    [Fact] void should_not_report_a_conflict() => _result.HasConflict.ShouldBeFalse();
    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_say_what_went_wrong() => _result.Problems.ShouldContain("could not be appended");
}
