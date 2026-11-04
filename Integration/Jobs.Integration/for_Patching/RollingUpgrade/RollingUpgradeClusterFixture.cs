// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.MongoDB;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Orleans.TestingHost;

namespace Cratis.Choreography.Jobs.Integration.for_Patching.RollingUpgrade;

/// <summary>
/// A cluster that starts as one release and has other releases join it while it runs, the way a rolling deployment does - nothing
/// is ever scaled down to make room.
/// </summary>
public sealed class RollingUpgradeClusterFixture : IDisposable
{
    /// <summary>
    /// How long to wait for a silo to join or for the cluster to settle.
    /// </summary>
    public static readonly TimeSpan Deadline = TimeSpan.FromSeconds(60);

    readonly MongoClient _client = new(JobsClusterFixture.ConnectionString);

    /// <summary>
    /// Initializes a new instance of the <see cref="RollingUpgradeClusterFixture"/> class.
    /// </summary>
    /// <param name="releases">The release each silo runs, by silo name; the first started is Primary, the next Secondary_1 and so on.</param>
    /// <param name="patchDuration">How long applying a patch takes.</param>
    /// <param name="responseTimeout">How long a grain call waits for its response.</param>
    /// <param name="silos">How many silos the cluster starts with.</param>
    public RollingUpgradeClusterFixture(IReadOnlyDictionary<string, Release> releases, TimeSpan? patchDuration = default, TimeSpan? responseTimeout = default, short silos = 1)
    {
        Signals = new($"choreography-rolling-upgrade-{Guid.NewGuid():N}", releases)
        {
            PatchDuration = patchDuration ?? TimeSpan.Zero,
            ResponseTimeout = responseTimeout ?? TimeSpan.FromSeconds(30)
        };
        RollingUpgradeClusterConfigurator.Signals = Signals;
        Cluster = new TestClusterBuilder(silos)
            .AddSiloBuilderConfigurator<RollingUpgradeClusterConfigurator>()
            .AddClientBuilderConfigurator<RollingUpgradeClusterConfigurator>()
            .Build();
        Cluster.Deploy();
    }

    /// <summary>
    /// Gets the cluster.
    /// </summary>
    public TestCluster Cluster { get; }

    /// <summary>
    /// Gets the signals recording what each silo applied.
    /// </summary>
    public RollingUpgradeSignals Signals { get; }

    /// <summary>
    /// Gets the patch storage every silo shares.
    /// </summary>
    public MongoDBPatchStorage Storage => new(_client, Options.Create(new MongoDBPatchStorageOptions { DatabaseName = Signals.Database }));

    /// <summary>
    /// Gets the names of the silos that are running.
    /// </summary>
    public IReadOnlyList<string> RunningSilos => [.. Cluster.Silos.Select(_ => _.Name)];

    /// <summary>
    /// Starts one more pod of a release while the others keep running. It only returns once the silo has finished starting, which
    /// includes applying its patches.
    /// </summary>
    /// <param name="label">What the pod is called in what was applied.</param>
    /// <param name="release">The release the pod runs.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Join(string label, Release release)
    {
        Signals.Joining = (label, release);
        try
        {
            await Cluster.StartAdditionalSiloAsync().WaitAsync(Deadline, TimeProvider.System);
        }
        finally
        {
            Signals.Joining = null;
        }
    }

    /// <summary>
    /// Kills a silo without letting it shut down, the way a crashed or evicted pod goes.
    /// </summary>
    /// <param name="name">The name of the silo.</param>
    /// <returns>Awaitable task.</returns>
    public Task Kill(string name) =>
        Cluster.KillSiloAsync(Cluster.Silos.Single(silo => silo.Name == name)).WaitAsync(Deadline, TimeProvider.System);

    /// <inheritdoc/>
    public void Dispose()
    {
        Cluster.StopAllSilos();
        Cluster.Dispose();
        _client.DropDatabase(Signals.Database);
        _client.Dispose();
    }
}
