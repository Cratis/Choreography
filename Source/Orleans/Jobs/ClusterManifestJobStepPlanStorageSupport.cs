// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs;

/// <summary>
/// Represents an <see cref="IJobStepPlanStorageSupport"/> that reads the support from the cluster manifest.
/// </summary>
/// <remarks>
/// A silo that lacks the support is one whose manifest lists the job class without
/// <see cref="JobStepPlanStorageSupportAttribute.PropertyName"/>, which is what a silo of an older release publishes. During a
/// rolling upgrade the plan therefore stays inline until the last older silo has left the cluster.
/// </remarks>
/// <param name="manifestProvider">The <see cref="IClusterManifestProvider"/> to read the manifest from.</param>
public class ClusterManifestJobStepPlanStorageSupport(IClusterManifestProvider manifestProvider) : IJobStepPlanStorageSupport
{
    /// <inheritdoc/>
    public bool IsSupportedByEverySilo(GrainType jobGrainType)
    {
        var silos = manifestProvider.Current.Silos;
        var hosting = 0;
        foreach (var manifest in silos.Values)
        {
            if (!manifest.Grains.TryGetValue(jobGrainType, out var properties))
            {
                continue;
            }

            hosting++;
            if (!properties.Properties.TryGetValue(JobStepPlanStorageSupportAttribute.PropertyName, out var value) ||
                value != JobStepPlanStorageSupportAttribute.PropertyValue)
            {
                return false;
            }
        }

        return hosting > 0;
    }
}
