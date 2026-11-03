// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Chronicle.Hosting;
using Cratis.Choreography.Chronicle.Integration.Documents;
using Cratis.Choreography.Chronicle.StateMachines;
using Cratis.Chronicle;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;

namespace Cratis.Choreography.Chronicle.Integration;

public class ClusterConfigurator : ISiloConfigurator, IClientBuilderConfigurator
{
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder.Services.AddCratisChoreographySerializers();

    public void Configure(ISiloBuilder siloBuilder)
    {
        var artifacts = Substitute.For<IClientArtifactsProvider>();
        artifacts.EventTypes.Returns([typeof(DocumentDrafted), typeof(DocumentSubmitted), typeof(DocumentWithdrawn)]);
        artifacts.Projections.Returns([]);
        artifacts.ModelBoundProjections.Returns([]);

        siloBuilder.Services.AddCratisChoreographySerializers();
        siloBuilder.Services.AddSingleton(artifacts);
        siloBuilder.Services.AddSingleton<IStateMachineEventStores>(InMemoryReadModels.Instance);
        siloBuilder.AddEventSourcedStateMachines(options =>
        {
            options.Discover = false;
            options.StateMachines.Add(typeof(DocumentLifecycle));
        });
    }
}
