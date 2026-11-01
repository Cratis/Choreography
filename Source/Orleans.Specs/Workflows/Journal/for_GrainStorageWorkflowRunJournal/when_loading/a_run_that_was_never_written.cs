// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Journal.for_GrainStorageWorkflowRunJournal.when_loading;

public class a_run_that_was_never_written : given.a_journal
{
    WorkflowRunJournalSnapshot _snapshot = null!;

    async Task Because() => _snapshot = await Journal.Load(Key);

    [Fact] void should_have_no_facts() => _snapshot.Facts.ShouldBeEmpty();
    [Fact] void should_end_before_the_first_position() => _snapshot.Tail.ShouldEqual(WorkflowJournalPosition.BeforeFirst);
}
