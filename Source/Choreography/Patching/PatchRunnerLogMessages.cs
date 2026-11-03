// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Choreography.Patching;

/// <summary>
/// Holds log messages for <see cref="PatchRunner"/>.
/// </summary>
internal static partial class PatchRunnerLogMessages
{
    [LoggerMessage(LogLevel.Critical, "Patch manager could not relocate to a host at least as new as {HostVersion}")]
    internal static partial void CouldNotRelocate(this ILogger<PatchRunner> logger, SemanticVersion hostVersion);

    [LoggerMessage(LogLevel.Warning, "The call to the patch manager failed for host {HostVersion}; asking again")]
    internal static partial void CallFailed(this ILogger<PatchRunner> logger, SemanticVersion hostVersion, Exception exception);

    [LoggerMessage(LogLevel.Critical, "Patching did not complete for host {HostVersion} within {Waited}")]
    internal static partial void DidNotComplete(this ILogger<PatchRunner> logger, SemanticVersion hostVersion, TimeSpan waited);
}
