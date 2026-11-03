// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Choreography.Workflows.Sweeping;

internal static partial class WorkflowRunSweeperLogging
{
    [LoggerMessage(LogLevel.Warning, "Workflow run '{Run}' could not be taken forward by the sweep")]
    internal static partial void RunCouldNotBeTended(this ILogger<WorkflowRunSweeper> logger, Exception exception, string run);
}
