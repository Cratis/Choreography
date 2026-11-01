// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Hosting;

/// <summary>
/// Represents where a co-hosted Orleans silo keeps grain state.
/// </summary>
public enum GrainStorageMode
{
    /// <summary>
    /// Durable grain state in MongoDB.
    /// </summary>
    MongoDB = 0,

    /// <summary>
    /// Grain state in memory, for a single silo in development or tests. Nothing survives the process.
    /// </summary>
    InMemory = 1
}
