// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Workflows.Journal.for_GrainStorageWorkflowRunJournal.when_appending;

public class while_another_write_lands_first : given.a_journal
{
    WorkflowJournalAppendResult _result = null!;

    void Establish() => Storage.LoseTheNextWrite = true;

    async Task Because() => _result = await Journal.Append(Key, WorkflowJournalPosition.BeforeFirst, [new WorkflowRunSucceeded()]);

    [Fact] void should_report_a_conflict() => _result.HasConflict.ShouldBeTrue();
}
