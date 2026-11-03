// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Hosting;

/// <summary>
/// Represents the instances of a type - those registered with the container and those the application defines but did not register.
/// </summary>
/// <typeparam name="T">The type of instance.</typeparam>
/// <param name="services">The <see cref="IServiceProvider"/> instances are resolved or created from.</param>
/// <param name="types">The <see cref="ITypes"/> that find the implementations.</param>
/// <remarks>
/// An implementation that is registered is used as registered; one that is not is created with the container supplying its
/// dependencies, each time the instances are enumerated.
/// </remarks>
sealed class DiscoveredInstances<T>(IServiceProvider services, ITypes types) : IInstancesOf<T>
    where T : class
{
    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        var registered = services.GetServices<T>().ToList();
        foreach (var instance in registered)
        {
            yield return instance;
        }

        foreach (var type in types.FindMultiple<T>().Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false } && registered.TrueForAll(instance => instance.GetType() != type)))
        {
            yield return (T)ActivatorUtilities.CreateInstance(services, type);
        }
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
