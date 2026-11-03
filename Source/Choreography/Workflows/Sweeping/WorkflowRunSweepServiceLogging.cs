// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Choreography.Workflows.Sweeping;

internal static partial class WorkflowRunSweepServiceLogging
{
    [LoggerMessage(LogLevel.Error, "The workflow run sweep could not complete")]
    internal static partial void SweepFailed(this ILogger<WorkflowRunSweepService> logger, Exception exception);
}
