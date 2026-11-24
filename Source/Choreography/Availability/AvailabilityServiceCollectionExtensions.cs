// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding the availability engine to a service collection.
/// </summary>
public static class AvailabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds the availability engine by delegating to <see cref="ArcAvailabilityServiceCollectionExtensions.AddAvailability"/> in Cratis.Arc.Availability.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration of <see cref="Cratis.Choreography.Availability.AvailabilityOptions"/>, such as the environment name.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// The overload resolution priority keeps existing calls binding here, so they report the obsolete warning instead of
    /// being ambiguous with the Arc method.
    /// </remarks>
    [OverloadResolutionPriority(1)]
    [Obsolete("The availability engine moved to Cratis.Arc.Availability (the Cratis.Arc.Availability package); call AddAvailability or AddArcAvailability from there. This forwarder is removed in the next major release.")]
    public static IServiceCollection AddAvailability(this IServiceCollection services, Action<Cratis.Choreography.Availability.AvailabilityOptions>? configure = null) =>
#pragma warning disable RCS1196 // Called as a static method: the instance form would bind back to this forwarder.
        ArcAvailabilityServiceCollectionExtensions.AddAvailability(services, options =>
#pragma warning restore RCS1196
        {
            // Arc builds its own options instance; apply the host's choices to a forwarder instance and copy them across.
            var forwarded = new Cratis.Choreography.Availability.AvailabilityOptions
            {
                EnvironmentName = options.EnvironmentName,
                GrantAllDeclaredAudiences = options.GrantAllDeclaredAudiences
            };
            configure?.Invoke(forwarded);
            options.EnvironmentName = forwarded.EnvironmentName;
            options.GrantAllDeclaredAudiences = forwarded.GrantAllDeclaredAudiences;
        });
}
