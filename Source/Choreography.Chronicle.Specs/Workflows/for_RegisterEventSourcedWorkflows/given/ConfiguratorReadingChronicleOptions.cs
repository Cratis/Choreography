// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows.for_RegisterEventSourcedWorkflows.given;

/// <summary>
/// Configures the workflow options from the Chronicle options, the way an application takes its event store from them.
/// </summary>
/// <param name="services">The <see cref="IServiceProvider"/> to read the Chronicle options from.</param>
/// <param name="failures">How many times configuring fails before it succeeds.</param>
public class ConfiguratorReadingChronicleOptions(IServiceProvider services, int failures = 0) : IConfigureOptions<EventSourcedWorkflowsOptions>
{
    /// <summary>
    /// The event store the configurator gives the workflow options.
    /// </summary>
    public const string EventStore = "store-from-chronicle";

    int _failuresLeft = failures;

    /// <inheritdoc/>
    public void Configure(EventSourcedWorkflowsOptions options)
    {
        _ = services.GetRequiredService<IOptions<ChronicleClientOptions>>().Value;
        _ = services.GetRequiredService<IOptions<ChronicleOptions>>().Value;
        if (_failuresLeft > 0)
        {
            _failuresLeft--;
            throw new InvalidOperationException("Configuring the workflow options failed");
        }

        options.EventStore = EventStore;
    }
}
