// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Availability;
using Cratis.Choreography.Availability.Audiences;
using Cratis.Choreography.Availability.Context;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding the availability engine to a service collection.
/// </summary>
public static class AvailabilityServiceCollectionExtensions
{
    /// <summary>
    /// Adds the availability engine. Register an <see cref="IAudienceSource"/>, <see cref="IDeclaredAudiences"/>,
    /// <see cref="AvailabilityCatalog"/> and <see cref="ContextValueCatalog"/> separately.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration of <see cref="AvailabilityOptions"/>, such as the environment name.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAvailability(this IServiceCollection services, Action<AvailabilityOptions>? configure = null)
    {
        var options = new AvailabilityOptions();
        configure?.Invoke(options);
        options.Validate();

        services.TryAddSingleton(options);
        services.TryAddSingleton<IAudienceResolver, AudienceResolver>();
        services.TryAddSingleton<IAvailabilityEvaluator, AvailabilityEvaluator>();
        return services;
    }
}
