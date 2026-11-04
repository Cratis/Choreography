// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Patching;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Storage;

/// <summary>
/// Extension methods for setting up in-memory patch storage.
/// </summary>
public static class InMemoryPatchStorageServiceCollectionExtensions
{
    /// <summary>
    /// Add in-memory patch storage for the patch manager.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChoreographyInMemoryPatchStorage(this IServiceCollection services)
    {
        services.AddSingleton<IPatchStorage, InMemoryPatchStorage>();
        return services;
    }
}
