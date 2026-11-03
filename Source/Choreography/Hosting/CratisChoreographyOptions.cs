// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;

namespace Cratis.Choreography.Hosting;

/// <summary>
/// Represents the options for the co-hosted Orleans silo.
/// </summary>
public class CratisChoreographyOptions
{
    /// <summary>
    /// Gets whether the silo is co-hosted at all.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets the clustering mode — localhost for single-instance development, MongoDB for durable membership.
    /// </summary>
    public ClusteringMode Clustering { get; init; } = ClusteringMode.Localhost;

    /// <summary>
    /// Gets the cluster id every instance joins.
    /// </summary>
    public string ClusterId { get; init; } = "default";

    /// <summary>
    /// Gets the service id every instance joins.
    /// </summary>
    public string ServiceId { get; init; } = "default";

    /// <summary>
    /// Gets the name of the MongoDB database holding cluster membership and grain state.
    /// </summary>
    public string DatabaseName { get; init; } = "orleans";

    /// <summary>
    /// Gets the MongoDB connection string to use.
    /// </summary>
    public string ConnectionString { get; init; } = "mongodb://localhost:27017";

    /// <summary>
    /// Gets where grain state - jobs, job steps and workflow runs - is kept. Defaults to MongoDB.
    /// </summary>
    /// <remarks>
    /// <see cref="GrainStorageMode.InMemory"/> with <see cref="ClusteringMode.Localhost"/> is a local configuration that needs no
    /// database: it suits development, tests and hosts such as a sandboxed runtime that run a single silo. Nothing survives the process.
    /// </remarks>
    public GrainStorageMode Storage { get; init; } = GrainStorageMode.MongoDB;

    /// <summary>
    /// Gets whether the workflow engine is hosted - its grains' storage, journal, sweep and node execution. Defaults to false.
    /// </summary>
    public bool Workflows { get; init; }

    /// <summary>
    /// Gets how the workflow engine is configured when <see cref="Workflows"/> is on.
    /// </summary>
    public Action<WorkflowsOptions>? ConfigureWorkflows { get; init; }

    /// <summary>
    /// Gets a <see cref="CratisChoreographyOptions"/> for running a single silo locally: localhost clustering and in-memory grain storage.
    /// </summary>
    /// <returns>The options.</returns>
    public static CratisChoreographyOptions Local() => new() { Clustering = ClusteringMode.Localhost, Storage = GrainStorageMode.InMemory };
}
