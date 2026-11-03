// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching;

/// <summary>
/// Exception that gets thrown when the patch manager does not move to a host at least as new as the one asking.
/// </summary>
/// <param name="hostVersion">The version of the host that asked.</param>
public class PatchManagerCouldNotRelocate(SemanticVersion hostVersion)
    : Exception($"The patch manager could not relocate to a host at least as new as {hostVersion}. Patches were not applied; an older host in the cluster is still holding the patch manager.");
