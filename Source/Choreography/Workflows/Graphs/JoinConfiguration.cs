// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// The configuration of a join node.
/// </summary>
/// <param name="ForEach">The <see cref="WorkflowNodeId"/> of the for-each whose branches it joins.</param>
/// <param name="Mode">The <see cref="JoinMode"/>.</param>
/// <param name="Threshold">How many branches must succeed, for <see cref="JoinMode.AtLeast"/>.</param>
public record JoinConfiguration(WorkflowNodeId ForEach, JoinMode Mode, int Threshold)
{
    /// <summary>
    /// Reads the configuration of a join node.
    /// </summary>
    /// <param name="configuration">The node's <see cref="WorkflowNodeConfiguration"/>.</param>
    /// <returns>The configuration, or null when it does not describe a join.</returns>
    public static JoinConfiguration? From(WorkflowNodeConfiguration configuration) =>
        EngineJson.TryRead<JoinConfiguration>(configuration.Value) is { } read && !string.IsNullOrEmpty(read.ForEach?.Value)
            ? read
            : null;
}
