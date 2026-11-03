// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// The exception that is thrown when what a workflow run decided could not be appended to its journal.
/// </summary>
/// <param name="run">The <see cref="WorkflowRunKey"/>.</param>
/// <param name="problems">What went wrong.</param>
public class WorkflowRunCouldNotBeRecorded(WorkflowRunKey run, string problems)
    : Exception($"What workflow run '{run}' decided could not be recorded: {problems}");
