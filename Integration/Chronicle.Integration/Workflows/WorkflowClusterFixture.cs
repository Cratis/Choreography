// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.TestingHost;

namespace Cratis.Orleans.Chronicle.Integration.Workflows;

/// <summary>
/// A real two-silo Orleans cluster hosting the workflow engine with its runs in <see cref="InMemoryEventLog"/>.
/// </summary>
public sealed class WorkflowClusterFixture : IDisposable
{
    public WorkflowClusterFixture()
    {
        Cluster = new TestClusterBuilder(2)
            .AddSiloBuilderConfigurator<WorkflowClusterConfigurator>()
            .AddClientBuilderConfigurator<WorkflowClusterConfigurator>()
            .Build();
        Cluster.Deploy();
    }

    public TestCluster Cluster { get; }

    public IGrainFactory GrainFactory => Cluster.GrainFactory;

    public void Dispose()
    {
        Cluster.StopAllSilos();
        Cluster.Dispose();
    }
}
