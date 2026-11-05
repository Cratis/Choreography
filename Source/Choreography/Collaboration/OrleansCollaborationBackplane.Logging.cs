// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Choreography.Collaboration;

/// <summary>
/// Structured log messages for <see cref="OrleansCollaborationBackplane"/>. All are best-effort diagnostics —
/// the cross-replica presence path self-heals, so failures are logged at debug and never surfaced.
/// </summary>
public sealed partial class OrleansCollaborationBackplane
{
    [LoggerMessage(LogLevel.Debug, Message = "Failed to unsubscribe the collaboration observer for group {GroupKey}")]
    static partial void LogUnsubscribeFailed(ILogger logger, Exception exception, string groupKey);

    [LoggerMessage(LogLevel.Debug, Message = "Failed to refresh the collaboration subscription for group {GroupKey}")]
    static partial void LogResubscribeFailed(ILogger logger, Exception exception, string groupKey);

    [LoggerMessage(LogLevel.Debug, Message = "Failed to vouch for the collaboration sessions of group {GroupKey}")]
    static partial void LogRefreshFailed(ILogger logger, Exception exception, string groupKey);

    [LoggerMessage(LogLevel.Debug, Message = "Failed to deliver a remote collaboration event to a local socket")]
    static partial void LogDeliveryFailed(ILogger logger, Exception exception);
}
