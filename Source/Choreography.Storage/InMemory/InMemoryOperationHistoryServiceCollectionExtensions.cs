// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Operations;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Storage;

/// <summary>
/// Extension methods for setting up in-memory operation history storage.
/// </summary>
public static class InMemoryOperationHistoryServiceCollectionExtensions
{
    /// <summary>
    /// Add in-memory storage for the operation history.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChoreographyInMemoryOperationHistory(this IServiceCollection services)
    {
        services.AddSingleton<IOperationHistoryStorage, InMemory.OperationHistoryStorage>();
        return services;
    }
}
