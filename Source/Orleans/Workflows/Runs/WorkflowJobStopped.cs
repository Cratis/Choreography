// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// A wave job a cancelled run still had work scheduled in was stopped.
/// </summary>
/// <remarks>
/// Which jobs a cancellation has to stop follows from the run's own history - every job holding
/// scheduled work when <see cref="WorkflowRunCancelled"/> was recorded - so the stop is owed until this is recorded.
/// </remarks>
/// <param name="Job">The stopped job.</param>
public record WorkflowJobStopped(WorkflowJobReference Job);
