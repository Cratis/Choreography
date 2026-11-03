// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching;

/// <summary>
/// Describes the outcome of requesting patch application.
/// </summary>
public enum PatchOutcome
{
    /// <summary>
    /// Patching completed, or was skipped because a newer host has already patched the store.
    /// </summary>
    Completed = 0,

    /// <summary>
    /// The patch manager is on an older host than the caller and moves away; ask again to reach a host at least as new as the caller.
    /// </summary>
    Relocate = 1
}
