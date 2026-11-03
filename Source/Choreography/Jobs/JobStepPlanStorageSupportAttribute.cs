// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Metadata;

namespace Cratis.Choreography.Jobs;

/// <summary>
/// Advertises in the cluster manifest that a silo can read a step plan kept outside the job document.
/// </summary>
/// <remarks>
/// The property lands in the grain properties of every job class, which every silo publishes in the cluster manifest. A
/// job only externalizes its plan when every silo that hosts the job class carries it, so a silo of a release without
/// it never meets a job that has an externalized plan.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class JobStepPlanStorageSupportAttribute : Attribute, IGrainPropertiesProviderAttribute
{
    /// <summary>
    /// The grain property that advertises the support.
    /// </summary>
    public const string PropertyName = "cratis.jobs.step-plan-storage";

    /// <summary>
    /// The value of <see cref="PropertyName"/> that this release advertises.
    /// </summary>
    public const string PropertyValue = "1";

    /// <inheritdoc/>
    public void Populate(IServiceProvider services, Type grainClass, GrainType grainType, Dictionary<string, string> properties) =>
        properties[PropertyName] = PropertyValue;
}
