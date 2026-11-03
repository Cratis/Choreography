// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Storage;

/// <summary>
/// Extension methods for setting up in-memory jobs storage.
/// </summary>
public static class InMemoryJobsStorageServiceCollectionExtensions
{
    /// <summary>
    /// Add in-memory jobs storage for the Orleans job system.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChoreographyInMemoryJobsStorage(this IServiceCollection services)
    {
        services.AddSingleton<IJobsStorage, InMemoryJobsStorage>();
        return services;
    }
}
