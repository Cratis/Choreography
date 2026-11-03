// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Choreography.Patching;

/// <summary>
/// Holds log messages for <see cref="PatchManager"/>.
/// </summary>
internal static partial class PatchManagerLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Starting patch application process")]
    internal static partial void StartingPatchApplication(this ILogger<PatchManager> logger);

    [LoggerMessage(LogLevel.Trace, "Current patched version: {Version}")]
    internal static partial void CurrentPatchedVersion(this ILogger<PatchManager> logger, SemanticVersion version);

    [LoggerMessage(LogLevel.Debug, "Found {Count} patches to apply")]
    internal static partial void FoundPatchesToApply(this ILogger<PatchManager> logger, int count);

    [LoggerMessage(LogLevel.Information, "Applying patch {PatchName} for version {Version} on host {HostVersion}")]
    internal static partial void ApplyingPatch(this ILogger<PatchManager> logger, string patchName, SemanticVersion version, SemanticVersion hostVersion);

    [LoggerMessage(LogLevel.Information, "Successfully applied patch {PatchName}")]
    internal static partial void PatchAppliedSuccessfully(this ILogger<PatchManager> logger, string patchName);

    [LoggerMessage(LogLevel.Error, "Failed to apply patch {PatchName}")]
    internal static partial void PatchApplicationFailed(this ILogger<PatchManager> logger, string patchName, Exception exception);

    [LoggerMessage(LogLevel.Trace, "Updated patched version to {Version}")]
    internal static partial void UpdatedPatchedVersion(this ILogger<PatchManager> logger, SemanticVersion version);

    [LoggerMessage(LogLevel.Debug, "Patch application process completed. Applied {AppliedCount} patches and skipped {SkippedCount} patches")]
    internal static partial void PatchApplicationCompleted(this ILogger<PatchManager> logger, int appliedCount, int skippedCount);

    [LoggerMessage(LogLevel.Warning, "Skipping patches on host {HostVersion}: host {RecordedVersion} has already patched this store")]
    internal static partial void SkippingPatchesForNewerHost(this ILogger<PatchManager> logger, SemanticVersion hostVersion, SemanticVersion recordedVersion);

    [LoggerMessage(LogLevel.Warning, "Host version {HostVersion} is unknown; bypassing the patch version gate without recording a host version")]
    internal static partial void UnknownHostVersion(this ILogger<PatchManager> logger, SemanticVersion hostVersion);
}
