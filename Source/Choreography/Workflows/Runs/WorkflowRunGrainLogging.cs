// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Choreography.Workflows.Runs;

/// <summary>
/// Log messages for <see cref="WorkflowRunGrain"/>.
/// </summary>
internal static partial class WorkflowRunGrainLogging
{
    [LoggerMessage(LogLevel.Information, "Workflow run '{Run}' moved on while a decision was being made - reading it again")]
    internal static partial void RunMovedOn(this ILogger<WorkflowRunGrain> logger, string run);

    [LoggerMessage(LogLevel.Warning, "Wave {Wave} of workflow run '{Run}' did not start ({Error}) - resuming the run reschedules it")]
    internal static partial void WaveDidNotStart(this ILogger<WorkflowRunGrain> logger, string run, int wave, string error);

    [LoggerMessage(LogLevel.Warning, "The index of active workflow runs could not be updated for run '{Run}'")]
    internal static partial void IndexCouldNotBeUpdated(this ILogger<WorkflowRunGrain> logger, Exception exception, string run);
}
