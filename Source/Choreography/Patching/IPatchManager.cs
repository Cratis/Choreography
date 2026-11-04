// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching;

/// <summary>
/// Defines the grain that applies pending patches. There is one per cluster.
/// </summary>
/// <remarks>
/// Hosts do not call this directly - <see cref="IPatchRunner"/> does, and keeps asking until the outcome is
/// <see cref="PatchOutcome.Completed"/>.
/// </remarks>
public interface IPatchManager : IGrainWithIntegerKey
{
    /// <summary>
    /// Applies every pending patch, unless the activation runs on an older host than the caller.
    /// </summary>
    /// <param name="caller">The version of the host asking for the patches to be applied.</param>
    /// <returns>
    /// The outcome - <see cref="PatchOutcome.Relocate"/> when the activation is on an older host than the caller and has
    /// started moving to the caller's silo, so the patches are never applied by a host that does not know the newest ones.
    /// </returns>
    /// <remarks>
    /// During a rolling upgrade a newer and an older host share the cluster. Only the newest host's patch set is complete, so
    /// the patch manager moves to it before it applies anything.
    /// </remarks>
    Task<PatchOutcome> ApplyPatchesFor(SemanticVersion caller);
}
