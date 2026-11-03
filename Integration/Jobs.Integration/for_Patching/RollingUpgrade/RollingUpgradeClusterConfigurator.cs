// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Hosting;
using Cratis.Choreography.Patching;
using Cratis.Choreography.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Orleans.Configuration;
using Orleans.TestingHost;

namespace Cratis.Choreography.Jobs.Integration.for_Patching.RollingUpgrade;

/// <summary>
/// Configures every silo of a rolling upgrade as the release its name says it runs, all sharing one patch store.
/// </summary>
public sealed class RollingUpgradeClusterConfigurator : ISiloConfigurator, IClientBuilderConfigurator
{
    /// <summary>
    /// Gets or sets the signals of the cluster being started.
    /// </summary>
    public static RollingUpgradeSignals Signals { get; set; } = null!;

    /// <inheritdoc/>
    public void Configure(IConfiguration configuration, IClientBuilder clientBuilder) => clientBuilder.Services.AddCratisChoreographySerializers();

    /// <inheritdoc/>
    public void Configure(ISiloBuilder siloBuilder)
    {
        var signals = Signals;
        var siloName = siloBuilder.Configuration["Orleans:Name"]
            ?? throw new InvalidOperationException("The test silo has no configured name.");
        var (label, release) = signals.Joining ?? (siloName, signals.ReleaseOf(siloName));

        siloBuilder.Services.AddCratisChoreographySerializers();
        siloBuilder.Services.AddSingleton<IInstancesOf<ICanApplyPatch>>(
            new ShippedPatches(release.Patches.Select(_ => new RecordedPatch<object>(_.Key, _.Value, label, signals))));
        siloBuilder.Services.AddCratisChoreographyMongoDBPatchStorage(
            new MongoClient(JobsClusterFixture.ConnectionString),
            options => options.DatabaseName = signals.Database);
        void Configure(PatchingOptions options)
        {
            options.HostVersion = release.Version;
            options.RelocationDelay = TimeSpan.FromMilliseconds(250);
            options.MaxAttempts = 8;
        }

        // A silo that does not patch on start still hosts the grain, so the manager can be placed on it.
        if (release.PatchesOnStart)
        {
            siloBuilder.AddCratisChoreographyPatching(Configure);
        }
        else
        {
            siloBuilder.AddCratisChoreographyPatchingServices(Configure);
        }
        siloBuilder.Configure<SiloMessagingOptions>(options => options.ResponseTimeout = signals.ResponseTimeout);
        siloBuilder.Configure<ClusterMembershipOptions>(options =>
        {
            options.ProbeTimeout = TimeSpan.FromSeconds(1);
            options.NumMissedProbesLimit = 2;
            options.NumVotesForDeathDeclaration = 1;
            options.TableRefreshTimeout = TimeSpan.FromSeconds(1);
            options.DeathVoteExpirationTimeout = TimeSpan.FromSeconds(10);
        });
    }
}
