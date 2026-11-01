// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Journal;

/// <summary>
/// The grain storage state of one run's journal.
/// </summary>
[GenerateSerializer]
public class WorkflowRunJournalState
{
    /// <summary>
    /// Gets or sets the facts, oldest first.
    /// </summary>
    [Id(0)]
    public IList<WorkflowStoredFact> Facts { get; set; } = new List<WorkflowStoredFact>();
}
