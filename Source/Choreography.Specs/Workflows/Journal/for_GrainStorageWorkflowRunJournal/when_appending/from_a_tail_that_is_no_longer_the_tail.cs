// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Journal.for_GrainStorageWorkflowRunJournal.when_appending;

public class from_a_tail_that_is_no_longer_the_tail : given.a_journal
{
    WorkflowJournalAppendResult _result = null!;
    WorkflowRunJournalSnapshot _snapshot = null!;

    async Task Establish() => await Journal.Append(Key, WorkflowJournalPosition.BeforeFirst, [new WorkflowJobStopped("job-1")]);

    async Task Because()
    {
        _result = await Journal.Append(Key, WorkflowJournalPosition.BeforeFirst, [new WorkflowRunSucceeded()]);
        _snapshot = await Journal.Load(Key);
    }

    [Fact] void should_report_a_conflict() => _result.HasConflict.ShouldBeTrue();
    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_record_nothing() => _snapshot.Facts.Count.ShouldEqual(1);
}
