// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Journal;

namespace Cratis.Choreography.Workflows.Runs.for_WorkflowRunGrain;

public class when_both_appends_conflict : given.a_run
{
    WorkflowRunCouldNotBeRecorded _error = null!;

    void Establish() => Journal.RejectAppend = WorkflowJournalAppendResult.Conflict;

    async Task Because() => _error = await Assert.ThrowsAsync<WorkflowRunCouldNotBeRecorded>(() => Grain.Admit(Definition, Trigger));

    [Fact] void should_reload_once() => Reads.ShouldEqual(2);
    [Fact] void should_not_retry_a_second_time() => Appends.ShouldEqual(2);
    [Fact] void should_not_record_an_admission() => History.ShouldBeEmpty();
    [Fact] void should_preserve_the_conflict_message() => _error.Message.ShouldEqual($"What workflow run '{Key}' decided could not be recorded: the run moved on twice while it was being decided");
}
