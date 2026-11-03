// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Patching;
using Cratis.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Placement;
using Orleans.Runtime.Placement;

namespace Cratis.Choreography.Patching;

/// <summary>
/// Represents an implementation of <see cref="IPatchManager"/>.
/// </summary>
/// <param name="storage"><see cref="IPatchStorage"/> holding which patches have been applied.</param>
/// <param name="patches">All available <see cref="ICanApplyPatch"/> instances.</param>
/// <param name="options">The <see cref="PatchingOptions"/> to use.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
/// <remarks>
/// <para>
/// The activation prefers the silo of its caller, so a host that starts first patches itself. Patches are applied in ascending
/// order of version, and a patch is recorded as applied only after it has run - so one that fails is attempted again on the
/// next start, and the ones before it are not repeated.
/// </para>
/// <para>
/// A store is only ever patched forward. A host older than the newest one that has patched the store applies nothing, and
/// an activation that finds its caller is on a newer host moves to the caller's silo instead of patching with a patch set that
/// may lack the newest patches.
/// </para>
/// </remarks>
[GrainType("cratis-choreography-patch-manager")]
[PreferLocalPlacement]
public class PatchManager(
    IPatchStorage storage,
    IInstancesOf<ICanApplyPatch> patches,
    IOptions<PatchingOptions> options,
    ILogger<PatchManager> logger) : Grain, IPatchManager
{
    readonly SemanticVersion _hostVersion = options.Value.HostVersion ?? HostVersion.Current;

    /// <inheritdoc/>
    public async Task<PatchOutcome> ApplyPatchesFor(SemanticVersion caller)
    {
        logger.PatchingRequested(caller, _hostVersion);
        var knownVersion = HostVersion.IsKnown(_hostVersion);
        if (knownVersion && HostVersion.IsKnown(caller) && _hostVersion < caller)
        {
            logger.RelocatingToCaller(caller, _hostVersion);
            return RelocateToCaller();
        }

        if (!knownVersion)
        {
            logger.UnknownHostVersion(_hostVersion);
        }
        else
        {
            var recorded = await storage.GetHostVersion();
            if (recorded is not null && recorded > _hostVersion)
            {
                logger.SkippingPatchesForNewerHost(_hostVersion, recorded);
                return PatchOutcome.Completed;
            }
        }

        await ApplyPendingPatches();
        if (knownVersion)
        {
            await storage.SetHostVersion(_hostVersion);
        }

        return PatchOutcome.Completed;
    }

    PatchOutcome RelocateToCaller()
    {
        if (PatchRelocation.CallerSilo() is { } callerSilo)
        {
            RequestContext.Set(IPlacementDirector.PlacementHintKey, callerSilo);
            this.MigrateOnIdle();
        }
        else
        {
            DeactivateOnIdle();
        }

        return PatchOutcome.Relocate;
    }

    async Task ApplyPendingPatches()
    {
        logger.StartingPatchApplication();

        var patchedVersion = await storage.GetPatchedVersion() ?? SemanticVersion.NotSet;
        logger.CurrentPatchedVersion(patchedVersion);

        var candidates = patches
            .Where(_ => _.Version > patchedVersion)
            .OrderBy(_ => _.Version)
            .ToList();

        var skipped = 0;
        var pending = new List<ICanApplyPatch>();
        foreach (var patch in candidates)
        {
            if (await storage.Has(patch.Name))
            {
                skipped++;
                continue;
            }

            pending.Add(patch);
        }

        if (pending.Count > 0)
        {
            logger.FoundPatchesToApply(pending.Count);
        }

        SemanticVersion? latestVersion = null;
        var applied = 0;
        foreach (var patch in pending)
        {
            try
            {
                logger.ApplyingPatch(patch.Name, patch.Version, _hostVersion);
                await patch.Up();
                await storage.Save(new AppliedPatch(patch.Name, patch.Version, DateTimeOffset.UtcNow));

                latestVersion = patch.Version;
                applied++;
                logger.PatchAppliedSuccessfully(patch.Name);
            }
            catch (Exception ex)
            {
                logger.PatchApplicationFailed(patch.Name, ex);
                throw;
            }
        }

        if (latestVersion is not null)
        {
            await storage.SetPatchedVersion(latestVersion);
            logger.UpdatedPatchedVersion(latestVersion);
        }

        logger.PatchApplicationCompleted(applied, skipped);
    }
}
