// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Hosting;
using Cratis.Choreography.Patching;
using Cratis.Choreography.Storage.Patching;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Jobs.Integration.for_Patching;

/// <summary>
/// A real co-hosted silo that applies patches when it starts, with everything in memory so it needs no database.
/// </summary>
/// <remarks>
/// Started through the public <c lang="csharp">AddCratisChoreography</c> surface exactly as an application would. The silo
/// has finished starting by the time the constructor returns, so what the patches did is what a host that waited for startup sees.
/// </remarks>
public class PatchingSiloFixture : IDisposable
{
    /// <summary>
    /// The version the silo claims to be, so the store gets marked with it.
    /// </summary>
    public static readonly SemanticVersion HostVersion = new(2, 0, 0);

    readonly WebApplication _app;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatchingSiloFixture"/> class.
    /// </summary>
    public PatchingSiloFixture()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["urls"] = "http://127.0.0.1:0";
        builder.AddCratisChoreography(new CratisChoreographyOptions
        {
            Clustering = ClusteringMode.Localhost,
            Storage = GrainStorageMode.InMemory,
            ClusterId = "integration-patching",
            ServiceId = "integration-patching",
            Patching = true,
            ConfigurePatching = options => options.HostVersion = HostVersion
        });
        builder.Services.AddSingleton<PatchProbe>();
        _app = builder.Build();
        _app.StartAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the probe the patches recorded themselves in.
    /// </summary>
    public PatchProbe Probe => _app.Services.GetRequiredService<PatchProbe>();

    /// <summary>
    /// Gets the storage the patch manager recorded the patches in.
    /// </summary>
    public IPatchStorage Storage => _app.Services.GetRequiredService<IPatchStorage>();

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _app.StopAsync().GetAwaiter().GetResult();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
