// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Journal.for_GrainStorageWorkflowRunJournal.when_appending;

public class after_facts_were_appended : given.a_journal
{
    WorkflowJournalAppendResult _result = null!;
    WorkflowRunJournalSnapshot _snapshot = null!;

    async Task Establish() => await Journal.Append(Key, WorkflowJournalPosition.BeforeFirst, [new WorkflowJobStopped("job-1")]);

    async Task Because()
    {
        _result = await Journal.Append(Key, new WorkflowJournalPosition(0), [new WorkflowRunSucceeded()]);
        _snapshot = await Journal.Load(Key);
    }

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_earlier_facts_first() => _snapshot.Facts.Select(fact => fact.Fact.GetType()).ShouldEqual([typeof(WorkflowJobStopped), typeof(WorkflowRunSucceeded)]);
}
