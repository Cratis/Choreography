// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Chronicle.Hosting;
using Cratis.Orleans.Chronicle.StateMachines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Cratis.Orleans.Chronicle.Integration.Workflows;

public class WorkflowClusterConfigurator : ISiloConfigurator, IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder.Services.AddCratisOrleansSerializers();

    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.Services.AddCratisOrleansSerializers();
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
