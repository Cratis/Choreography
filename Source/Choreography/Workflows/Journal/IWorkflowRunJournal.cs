// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Journal;

/// <summary>
/// Defines where the facts of workflow runs are kept - the run grain's only state.
/// </summary>
/// <remarks>
/// <para>
/// A run grain keeps nothing but what it can read back from here: on activation it loads every fact of its run
/// and folds them, and everything it decides afterwards it appends, naming the tail it decided from. An append
/// whose tail is no longer the journal's tail records nothing and says so, so two activations of one run - or a
/// call racing a restart - can never both record a decision made from the same history.
/// </para>
/// <para>
/// The core package ships a journal on Orleans grain storage. The Chronicle adapter ships one on the event log.
/// </para>
/// </remarks>
public interface IWorkflowRunJournal
{
    /// <summary>
    /// Loads every fact of a run, and the position they end at.
    /// </summary>
    /// <param name="key">The <see cref="WorkflowRunKey"/> of the run.</param>
    /// <returns>The <see cref="WorkflowRunJournalSnapshot"/> - empty for a run that has no facts.</returns>
    Task<WorkflowRunJournalSnapshot> Load(WorkflowRunKey key);

    /// <summary>
    /// Appends facts to a run, provided the journal still ends where the caller last saw it end.
    /// </summary>
    /// <param name="key">The <see cref="WorkflowRunKey"/> of the run.</param>
    /// <param name="expectedTail">The <see cref="WorkflowJournalPosition"/> the decision was made from.</param>
    /// <param name="facts">The facts to record, in order. All of them are recorded or none are.</param>
    /// <returns>The <see cref="WorkflowJournalAppendResult"/>.</returns>
    Task<WorkflowJournalAppendResult> Append(WorkflowRunKey key, WorkflowJournalPosition expectedTail, IReadOnlyList<object> facts);
}
