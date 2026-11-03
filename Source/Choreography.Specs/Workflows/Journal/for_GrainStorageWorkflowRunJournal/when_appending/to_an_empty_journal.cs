// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Journal.for_GrainStorageWorkflowRunJournal.when_appending;

public class to_an_empty_journal : given.a_journal
{
    WorkflowJournalAppendResult _result = null!;
    WorkflowRunJournalSnapshot _snapshot = null!;

    async Task Because()
    {
        _result = await Journal.Append(Key, WorkflowJournalPosition.BeforeFirst, [new WorkflowJobStopped("job-1"), new WorkflowRunFailed("it failed")]);
        _snapshot = await Journal.Load(Key);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_end_at_the_last_fact() => _result.Tail.ShouldEqual(new WorkflowJournalPosition(1));
    [Fact] void should_read_the_facts_back_in_order() => _snapshot.Facts.Select(fact => fact.Fact).ShouldEqual([new WorkflowJobStopped("job-1"), new WorkflowRunFailed("it failed")]);
    [Fact] void should_read_the_same_tail_back() => _snapshot.Tail.ShouldEqual(_result.Tail);
    [Fact] void should_record_when_the_facts_were_appended() => _snapshot.Facts.All(fact => fact.Occurred > DateTimeOffset.UtcNow.AddMinutes(-1)).ShouldBeTrue();
}
