// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Graphs;

/// <summary>
/// The configuration of a for-each node.
/// </summary>
/// <param name="ItemsFrom">The <see cref="WorkflowNodeId"/> whose output holds the items.</param>
/// <param name="ItemsProperty">The property of that output holding the list. Every item is an object with a unique string <c lang="csharp">key</c>.</param>
/// <param name="MaxParallelism">How many branch executions may be scheduled at once.</param>
public record ForEachConfiguration(WorkflowNodeId ItemsFrom, string ItemsProperty, int MaxParallelism)
{
    /// <summary>
    /// Reads the configuration of a for-each node.
    /// </summary>
    /// <param name="configuration">The node's <see cref="WorkflowNodeConfiguration"/>.</param>
    /// <returns>The configuration, or null when it does not describe a for-each.</returns>
    public static ForEachConfiguration? From(WorkflowNodeConfiguration configuration) =>
        EngineJson.TryRead<ForEachConfiguration>(configuration.Value) is { } read
            && !string.IsNullOrEmpty(read.ItemsFrom?.Value)
            && !string.IsNullOrEmpty(read.ItemsProperty)
            ? read with { MaxParallelism = Math.Max(1, read.MaxParallelism) }
            : null;
}
