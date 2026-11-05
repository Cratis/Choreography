// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Collaboration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Choreography.Hosting;

/// <summary>
/// Extension methods for adding live collaboration to an application.
/// </summary>
public static class CollaborationServiceCollectionExtensions
{
    /// <summary>
    /// Adds live collaboration: the <see cref="ICollaborationHub"/>, the <see cref="ICollaborationParticipantResolver"/>
    /// that decides who connects to which room, and the single-replica <see cref="LocalCollaborationBackplane"/>.
    /// </summary>
    /// <typeparam name="TResolver">The application's <see cref="ICollaborationParticipantResolver"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <param name="configure">Optional callback for configuring the <see cref="CollaborationOptions"/>.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// The resolver is registered as scoped, because it is resolved per upgrade request and typically depends on the
    /// current user. Call <see cref="AddCratisCollaborationOrleans(IServiceCollection)"/> as well when the application runs at more than one
    /// replica, then map the endpoint with <see cref="CollaborationEndpointExtensions.MapCratisCollaboration"/>.
    /// </remarks>
    public static IServiceCollection AddCratisCollaboration<TResolver>(this IServiceCollection services, Action<CollaborationOptions>? configure = default)
        where TResolver : class, ICollaborationParticipantResolver
    {
        services.AddOptions<CollaborationOptions>().Configure(options => configure?.Invoke(options));
        services.Replace(ServiceDescriptor.Scoped<ICollaborationParticipantResolver, TResolver>());
        services.TryAddSingleton<ICollaborationBackplane, LocalCollaborationBackplane>();
        services.TryAddSingleton<ICollaborationHub, CollaborationHub>();
        return services;
    }

    /// <summary>
    /// Shares the roster across the replicas of an application through an Orleans grain, replacing the single-replica
    /// backplane. Call it only when the application hosts or connects to an Orleans cluster, and after
    /// <see cref="AddCratisCollaboration{TResolver}"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns><see cref="IServiceCollection"/> for continuation.</returns>
    /// <remarks>
    /// The backplane is also a hosted service that keeps the replica's grain subscriptions alive and vouches for its
    /// sessions, so the replica must run with an <c language="csharp">IGrainFactory</c>, that is a silo or a cluster client.
    /// </remarks>
    public static IServiceCollection AddCratisCollaborationOrleans(this IServiceCollection services)
    {
        services.Replace(ServiceDescriptor.Singleton<OrleansCollaborationBackplane, OrleansCollaborationBackplane>());
        services.Replace(ServiceDescriptor.Singleton<ICollaborationBackplane>(serviceProvider => serviceProvider.GetRequiredService<OrleansCollaborationBackplane>()));
        services.AddHostedService(serviceProvider => serviceProvider.GetRequiredService<OrleansCollaborationBackplane>());
        return services;
    }

    /// <summary>
    /// Shares the roster across the replicas of an application through an Orleans grain, replacing the single-replica
    /// backplane. See <see cref="AddCratisCollaborationOrleans(IServiceCollection)"/>.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    /// <returns>The silo builder for chaining.</returns>
    public static ISiloBuilder AddCratisCollaborationOrleans(this ISiloBuilder silo)
    {
        silo.Services.AddCratisCollaborationOrleans();
        return silo;
    }
}
