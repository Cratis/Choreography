// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Journal;

/// <summary>
/// A run's facts as they are, and where they end.
/// </summary>
/// <param name="Facts">The facts, oldest first.</param>
/// <param name="Tail">The <see cref="WorkflowJournalPosition"/> of the last one.</param>
public record WorkflowRunJournalSnapshot(IReadOnlyList<WorkflowRecordedFact> Facts, WorkflowJournalPosition Tail)
{
    /// <summary>
    /// The snapshot of a run that has no facts.
    /// </summary>
    public static readonly WorkflowRunJournalSnapshot Empty = new([], WorkflowJournalPosition.BeforeFirst);
}
