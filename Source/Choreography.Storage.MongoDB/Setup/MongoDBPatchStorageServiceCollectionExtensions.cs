// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.MongoDB;
using Cratis.Choreography.Storage.Patching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Cratis.Choreography.Setup;

/// <summary>
/// Extension methods for setting up MongoDB patch storage.
/// </summary>
public static class MongoDBPatchStorageServiceCollectionExtensions
{
    /// <summary>
    /// Add MongoDB patch storage for the patch manager.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="client">
    /// The <see cref="IMongoClient"/> to use. The caller owns it and its pool settings; share one client for the whole process.
    /// A client already registered, for example by the MongoDB jobs storage, is kept.
    /// </param>
    /// <param name="configure">Optional callback for configuring <see cref="MongoDBPatchStorageOptions"/>.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChoreographyMongoDBPatchStorage(
        this IServiceCollection services,
        IMongoClient client,
        Action<MongoDBPatchStorageOptions>? configure = default)
    {
        services.TryAddSingleton(client);
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddOptions<MongoDBPatchStorageOptions>();
        services.AddSingleton<IPatchStorage, MongoDBPatchStorage>();
        return services;
    }
}
