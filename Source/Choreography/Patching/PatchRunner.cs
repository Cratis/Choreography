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
    /// The prefix of the message Orleans gives a call that got no response in time.
    /// </summary>
    internal const string ResponseTimeoutPrefix = "Response did not arrive on time";

    /// <summary>
    /// The prefix of the failure Orleans raises while the cluster's manifests have not yet converged on a silo that can host the patch manager.
    /// </summary>
    internal const string PlacementFailurePrefix = "No active nodes are compatible with grain cratis-choreography-patch-manager and interface Cratis.Choreography.Patching.IPatchManager version ";

    static readonly TimeSpan _maxDelay = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    public async Task ApplyPatches(CancellationToken cancellationToken = default)
    {
        var hostVersion = options.Value.HostVersion ?? HostVersion.Current;
        var manager = grainFactory.GetGrain<IPatchManager>(0);
        var started = timeProvider.GetUtcNow();
        var relocations = 0;
        var delay = options.Value.RelocationDelay;
        Exception? lastFailure = null;

        RequestContext.Set(PatchRelocation.CallerSiloKey, localSiloDetails.SiloAddress);
        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var waitingForRelocation = false;
                try
                {
                    if (await manager.ApplyPatchesFor(hostVersion) == PatchOutcome.Completed) return;
                    waitingForRelocation = true;
                }
                catch (OrleansException exception) when (IsAwaitingManifest(exception))
                {
                    // The placement can be asked before the new silo's manifest has converged.
                    waitingForRelocation = true;
                    lastFailure = exception;
                }
                catch (Exception exception) when (IsTransient(exception))
                {
                    // The call failed, not the patch: it timed out while a long patch kept running, or the silo holding the
                    // manager is gone. Patches are recorded only once they have run, so asking again either waits for the one
                    // still running or reaches a manager on a silo that is alive.
                    lastFailure = exception;
                    logger.CallFailed(hostVersion, exception);
                }

                if (waitingForRelocation && ++relocations >= options.Value.MaxAttempts)
                {
                    logger.CouldNotRelocate(hostVersion);
                    throw new PatchManagerCouldNotRelocate(hostVersion);
                }

                var waited = timeProvider.GetUtcNow() - started;
                if (waited >= options.Value.MaxWait)
                {
                    logger.DidNotComplete(hostVersion, waited);
                    throw new PatchingDidNotComplete(hostVersion, waited, lastFailure);
                }

                await Task.Delay(delay, timeProvider, cancellationToken);
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, _maxDelay.Ticks));
            }
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

    /// <summary>
    /// Recognizes a call that failed for reasons of transport rather than because a patch failed.
    /// </summary>
    /// <param name="exception">The exception the call raised.</param>
    /// <returns>Whether asking again can succeed.</returns>
    /// <remarks>
    /// A timeout counts only when it is Orleans' own response timeout: a patch that throws a <see cref="TimeoutException"/> of its own
    /// is a failing patch, and fails the start.
    /// </remarks>
    internal static bool IsTransient(Exception exception) => exception switch
    {
        SiloUnavailableException => true,
        OrleansMessageRejectionException => true,
        TimeoutException timeout => timeout.Message.StartsWith(ResponseTimeoutPrefix, StringComparison.Ordinal),
        _ => false
    };
}
