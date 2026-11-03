// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Journal;

namespace Cratis.Choreography.Workflows.Runs.for_WorkflowRunGrain.given;

public class a_journal : IWorkflowRunJournal
{
    public List<WorkflowRecordedFact> Facts { get; } = [];

    public int Reads { get; private set; }

    public int Appends { get; private set; }

    public bool ConflictOnFirstAppend { get; set; }

    public Task<WorkflowRunJournalSnapshot> Load(WorkflowRunKey key)
    {
        Reads++;
        return Task.FromResult(new WorkflowRunJournalSnapshot([.. Facts], Tail));
    }

    public Task<WorkflowJournalAppendResult> Append(WorkflowRunKey key, WorkflowJournalPosition expectedTail, IReadOnlyList<object> facts)
    {
        Appends++;
        if (ConflictOnFirstAppend && Appends == 1)
        {
            // Someone else got there first: the journal has moved on from what the caller decided from.
            return Task.FromResult(WorkflowJournalAppendResult.Conflict(expectedTail));
        }

        if (expectedTail != Tail)
        {
            return Task.FromResult(WorkflowJournalAppendResult.Conflict(expectedTail));
        }

        Facts.AddRange(facts.Select(fact => new WorkflowRecordedFact(fact, DateTimeOffset.UtcNow)));
        return Task.FromResult(WorkflowJournalAppendResult.Appended(Tail));
    }

    WorkflowJournalPosition Tail => new(Facts.Count - 1);
}
