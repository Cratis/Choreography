// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching;

/// <summary>
/// Defines the service a host uses to apply pending patches.
/// </summary>
/// <remarks>
/// <see cref="Hosting.PatchingSiloBuilderExtensions.AddCratisChoreographyPatching(ISiloBuilder, Action{PatchingOptions}?)"/> runs it
/// when the silo becomes active. A host that has to patch at a precise point of its own startup - after it has prepared what
/// its patches rely on - registers it without that and calls it from its own startup.
/// </remarks>
public interface IPatchRunner
{
    /// <summary>
    /// Applies every pending patch and returns when the patch manager has completed.
    /// </summary>
    /// <param name="cancellationToken">Token for cancelling the wait.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="PatchManagerCouldNotRelocate">Thrown when no host at least as new as this one takes over patching within the allowed attempts.</exception>
    /// <remarks>
    /// When a patch throws, this call throws and the host does not start: a host that carries on with a half-applied change
    /// would run on data it was not written for.
    /// </remarks>
    Task ApplyPatches(CancellationToken cancellationToken = default);
}
