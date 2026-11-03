// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Choreography.Operations;

/// <summary>
/// Extension methods for setting up operation history and the undo/redo engine.
/// </summary>
public static class OperationsServiceCollectionExtensions
{
    /// <summary>
    /// Add the operation history and undo/redo engine. The host registers an <see cref="IOperationExecutor"/> and
    /// an <see cref="IOperationHistoryStorage"/>, for example with AddCratisChoreographyInMemoryOperationHistory.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    public static IServiceCollection AddCratisChoreographyOperations(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IOperationHistory, OperationHistory>();
        services.TryAddSingleton<IOperationDefinitions, OperationDefinitions>();
        services.TryAddSingleton<OperationLocks>();
        services.TryAddScoped<IOperationEngine, OperationEngine>();
        return services;
    }
}
