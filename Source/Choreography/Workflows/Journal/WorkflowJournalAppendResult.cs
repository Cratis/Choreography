// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Journal;

/// <summary>
/// The result of appending to a run's journal.
/// </summary>
/// <param name="IsSuccess">Whether the facts were recorded.</param>
/// <param name="HasConflict">Whether the journal had moved on from the expected tail, so nothing was recorded.</param>
/// <param name="Tail">The tail after the append - the expected one when nothing was recorded.</param>
/// <param name="Problems">What went wrong, when the append failed for a reason other than a conflict.</param>
public record WorkflowJournalAppendResult(bool IsSuccess, bool HasConflict, WorkflowJournalPosition Tail, string Problems)
{
    /// <summary>
    /// Creates the result of facts that were recorded.
    /// </summary>
    /// <param name="tail">The tail after the append.</param>
    /// <returns>The <see cref="WorkflowJournalAppendResult"/>.</returns>
    public static WorkflowJournalAppendResult Appended(WorkflowJournalPosition tail) => new(true, false, tail, string.Empty);

    /// <summary>
    /// Creates the result of an append that lost to another one.
    /// </summary>
    /// <param name="tail">The tail that was expected.</param>
    /// <returns>The <see cref="WorkflowJournalAppendResult"/>.</returns>
    public static WorkflowJournalAppendResult Conflict(WorkflowJournalPosition tail) => new(false, true, tail, string.Empty);

    /// <summary>
    /// Creates the result of an append that could not be recorded.
    /// </summary>
    /// <param name="tail">The tail that was expected.</param>
    /// <param name="problems">What went wrong.</param>
    /// <returns>The <see cref="WorkflowJournalAppendResult"/>.</returns>
    public static WorkflowJournalAppendResult Failed(WorkflowJournalPosition tail, string problems) => new(false, false, tail, problems);
}
