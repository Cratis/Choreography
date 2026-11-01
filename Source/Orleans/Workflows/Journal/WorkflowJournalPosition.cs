// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Journal;

/// <summary>
/// The position of the last fact in a run's journal.
/// </summary>
/// <param name="Value">The underlying value - the position of the last fact, or -1 when the journal is empty.</param>
/// <remarks>
/// A journal decides what a position means; a caller only hands back the one it was given. That is what lets
/// an append name the history a decision was made from.
/// </remarks>
public record WorkflowJournalPosition(long Value)
{
    /// <summary>
    /// The position of a journal that holds no facts.
    /// </summary>
    public static readonly WorkflowJournalPosition BeforeFirst = new(-1);
}
