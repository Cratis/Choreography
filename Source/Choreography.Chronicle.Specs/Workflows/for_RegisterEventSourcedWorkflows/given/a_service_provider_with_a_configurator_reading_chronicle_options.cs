// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.Hosting;
using Cratis.Choreography.Chronicle.StateMachines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Chronicle.Workflows.for_RegisterEventSourcedWorkflows.given;

public class a_service_provider_with_a_configurator_reading_chronicle_options : Specification
{
    protected ServiceProvider _provider = null!;

    protected virtual int ConfiguratorFailures => 0;

    void Establish() => _provider = BuildProvider(ConfiguratorFailures);

    void Destroy() => _provider.Dispose();

    protected static ServiceProvider BuildProvider(int configuratorFailures)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton(Substitute.For<IGrainFactory>())
            .AddSingleton(Substitute.For<IStateMachineEventStores>());
        services.AddSingleton<IConfigureOptions<EventSourcedWorkflowsOptions>>(provider => new ConfiguratorReadingChronicleOptions(provider, configuratorFailures));
        services.AddEventSourcedWorkflows(options => options.ReadModels = false);
        return services.BuildServiceProvider();
    }
}
