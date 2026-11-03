// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Choreography.Chronicle.Workflows;

internal static partial class WorkflowScheduleServiceLogging
{
    [LoggerMessage(LogLevel.Error, "Scheduled workflows could not be started")]
    internal static partial void ScheduleFailed(this ILogger<WorkflowScheduleService> logger, Exception exception);
}
