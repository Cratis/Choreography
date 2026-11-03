// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Patching;

/// <summary>
/// Represents an implementation of <see cref="IPatchRunner"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting the patch manager.</param>
/// <param name="localSiloDetails">The <see cref="ILocalSiloDetails"/> naming the silo the patch manager should prefer.</param>
/// <param name="options">The <see cref="PatchingOptions"/> to use.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/> the wait between attempts is measured on.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <remarks>
/// During a rolling upgrade the patch manager can be active on an older host. It then moves to the silo of this host and
/// answers <see cref="PatchOutcome.Relocate"/>; the runner keeps asking, backing off exponentially, until it completes.
/// </remarks>
public class PatchRunner(
    IGrainFactory grainFactory,
    ILocalSiloDetails localSiloDetails,
    IOptions<PatchingOptions> options,
    TimeProvider timeProvider,
    ILogger<PatchRunner> logger) : IPatchRunner
{
    /// <summary>
    /// The prefix of the failure Orleans raises while the cluster's manifests have not yet converged on a silo that can host the patch manager.
    /// </summary>
    internal const string PlacementFailurePrefix = "No active nodes are compatible with grain cratis-choreography-patch-manager and interface Cratis.Choreography.Patching.IPatchManager version ";

    /// <inheritdoc/>
    public async Task ApplyPatches(CancellationToken cancellationToken = default)
    {
        var hostVersion = options.Value.HostVersion ?? HostVersion.Current;
        var manager = grainFactory.GetGrain<IPatchManager>(0);

        RequestContext.Set(PatchRelocation.CallerSiloKey, localSiloDetails.SiloAddress);
        try
        {
            var delay = options.Value.RelocationDelay;
            for (var attempt = 0; attempt < options.Value.MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (await manager.ApplyPatchesFor(hostVersion) == PatchOutcome.Completed) return;
                }
                catch (OrleansException exception) when (IsAwaitingManifest(exception))
                {
                    // The placement can be asked before the new silo's manifest has converged.
                }

                if (attempt < options.Value.MaxAttempts - 1)
                {
                    await Task.Delay(delay, timeProvider, cancellationToken);
                    delay += delay;
                }
            }

            logger.CouldNotRelocate(hostVersion);
            throw new PatchManagerCouldNotRelocate(hostVersion);
        }
        finally
        {
            RequestContext.Remove(PatchRelocation.CallerSiloKey);
        }
    }

    /// <summary>
    /// Recognizes only the patch manager's no-compatible-silo response during manifest convergence.
    /// </summary>
    /// <param name="exception">The Orleans placement failure.</param>
    /// <returns>Whether asking again can succeed.</returns>
    internal static bool IsAwaitingManifest(OrleansException exception) =>
        exception.Message.StartsWith(PlacementFailurePrefix, StringComparison.Ordinal);
}
