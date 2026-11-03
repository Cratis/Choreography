// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.Patching;
using Cratis.Choreography.Storage.Sql;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Setup;

/// <summary>
/// Extension methods for setting up SQL patch storage.
/// </summary>
public static class SqlPatchStorageServiceCollectionExtensions
{
    /// <summary>
    /// Add SQL patch storage for the patch manager.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">Callback for configuring <see cref="SqlPatchStorageOptions"/>.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChoreographySqlPatchStorage(
        this IServiceCollection services,
        Action<SqlPatchStorageOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IPatchStorage, SqlPatchStorage>();
        return services;
    }
}
