// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching;

/// <summary>
/// Represents the options for applying patches when a host starts.
/// </summary>
public class PatchingOptions
{
    /// <summary>
    /// Gets or sets the version of the running host. Defaults to the informational version of the entry assembly.
    /// </summary>
    /// <remarks>
    /// The patch manager only applies patches on a host at least as new as every host that has patched the store, and moves
    /// to a newer caller's silo during a rolling upgrade, so this version decides who patches. A version that is not set, or is the
    /// 1.0.0 an unversioned build carries, is unknown: patches are applied but the store is not marked with it.
    /// </remarks>
    public SemanticVersion? HostVersion { get; set; }

    /// <summary>
    /// Gets or sets how many times a host asks the patch manager to apply patches while it is waiting for the manager to move
    /// to a host at least as new as itself, before it gives up and fails the start. Defaults to 5.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets how long a host waits before asking again, doubled after every attempt. Defaults to one second.
    /// </summary>
    /// <remarks>
    /// The older activation deactivates once idle, and under load that can take longer than a short fixed wait allows.
    /// </remarks>
    public TimeSpan RelocationDelay { get; set; } = TimeSpan.FromSeconds(1);
}
