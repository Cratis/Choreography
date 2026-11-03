// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.Hosting;
using Cratis.Choreography.Chronicle.StateMachines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Cratis.Choreography.Chronicle.Integration.Workflows;

public class WorkflowClusterConfigurator : ISiloConfigurator, IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder.Services.AddCratisChoreographySerializers();

    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddCratisChoreographySerializers();
        siloBuilder.AddMemoryGrainStorage(WellKnownGrainStorageProviders.Workflows);
        siloBuilder.UseInMemoryReminderService();
        siloBuilder.Services.AddSingleton<IStateMachineEventStores>(InMemoryEventLog.Instance);
        siloBuilder.AddEventSourcedWorkflows(options =>
        {
            options.EventStore = "workflows";
            options.ReadModels = false;
        });
    }
}
