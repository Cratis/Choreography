// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Choreography.Hosting;

/// <summary>
/// Extension methods for applying patches when a silo starts.
/// </summary>
public static class PatchingSiloBuilderExtensions
{
    /// <summary>
    /// Adds patching: discovered <see cref="ICanApplyPatch"/> implementations are applied once per store when the silo becomes active.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    /// <param name="configure">Optional callback for configuring <see cref="PatchingOptions"/>.</param>
    /// <returns>The silo builder for chaining.</returns>
    /// <remarks>
    /// Needs an <see cref="Storage.Patching.IPatchStorage"/> - add one with
    /// <c lang="csharp">AddCratisChoreographyInMemoryPatchStorage</c>, <c lang="csharp">AddCratisChoreographyMongoDBPatchStorage</c>
    /// or <c lang="csharp">AddCratisChoreographySqlPatchStorage</c>. Patching throws when the silo starts without one.
    /// Patches cross a grain boundary as plain Cratis types, so the silo needs the serializers
    /// <c lang="csharp">AddCratisChoreographySerializers</c> registers - co-hosting with
    /// <c lang="csharp">AddCratisChoreography</c> already does.
    /// </remarks>
    public static ISiloBuilder AddCratisChoreographyPatching(this ISiloBuilder silo, Action<PatchingOptions>? configure = default)
    {
        silo.AddCratisChoreographyPatchingServices(configure);
        silo.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILifecycleParticipant<ISiloLifecycle>, PatchingStartupTask>());
        return silo;
    }

    /// <summary>
    /// Adds patching without applying it when the silo starts, for a host that calls <see cref="IPatchRunner"/> itself.
    /// </summary>
    /// <param name="silo">The silo builder.</param>
    /// <param name="configure">Optional callback for configuring <see cref="PatchingOptions"/>.</param>
    /// <returns>The silo builder for chaining.</returns>
    public static ISiloBuilder AddCratisChoreographyPatchingServices(this ISiloBuilder silo, Action<PatchingOptions>? configure = default)
    {
        if (configure is not null)
        {
            silo.Services.Configure(configure);
        }

        silo.Services.AddOptions<PatchingOptions>();
        silo.Services.TryAddSingleton(TimeProvider.System);
        silo.Services.TryAddSingleton<ITypes>(_ => new Cratis.Types.Types());
        silo.Services.TryAddSingleton<IPatchRunner, PatchRunner>();
        silo.Services.TryAddTransient<IInstancesOf<ICanApplyPatch>, DiscoveredInstances<ICanApplyPatch>>();
        return silo;
    }
}
