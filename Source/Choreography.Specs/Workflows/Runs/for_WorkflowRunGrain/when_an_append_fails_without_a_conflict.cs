// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Journal;

namespace Cratis.Choreography.Workflows.Runs.for_WorkflowRunGrain;

public class when_an_append_fails_without_a_conflict : given.a_run
{
    WorkflowRunCouldNotBeRecorded _error = null!;

    void Establish() => Journal.RejectAppend = tail => WorkflowJournalAppendResult.Failed(tail, "storage is unavailable");

    async Task Because() => _error = await Assert.ThrowsAsync<WorkflowRunCouldNotBeRecorded>(() => Grain.Admit(Definition, Trigger));

    [Fact] void should_not_reload() => Reads.ShouldEqual(1);
    [Fact] void should_not_retry() => Appends.ShouldEqual(1);
    [Fact] void should_not_record_an_admission() => History.ShouldBeEmpty();
    [Fact] void should_preserve_the_storage_message() => _error.Message.ShouldEqual($"What workflow run '{Key}' decided could not be recorded: storage is unavailable");
}
