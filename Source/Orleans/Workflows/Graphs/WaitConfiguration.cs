// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Waits;

namespace Cratis.Orleans.Workflows.Graphs;

/// <summary>
/// The configuration of a wait or approval node.
/// </summary>
/// <param name="Kind">The <see cref="WaitKind"/>.</param>
/// <param name="After">How long a timer waits.</param>
/// <param name="ExpiresAfter">How long a signal or approval waits before it expires - zero waits forever.</param>
/// <param name="Signal">The <see cref="WorkflowContractReference"/> of the signal a signal wait accepts.</param>
public record WaitConfiguration(WaitKind Kind, TimeSpan After, TimeSpan ExpiresAfter, WorkflowContractReference Signal)
{
    /// <summary>
    /// Reads the configuration of a wait node.
    /// </summary>
    /// <param name="configuration">The node's <see cref="WorkflowNodeConfiguration"/>.</param>
    /// <returns>The configuration, or null when it does not describe a wait.</returns>
    public static WaitConfiguration? From(WorkflowNodeConfiguration configuration) =>
        EngineJson.TryRead<WaitConfiguration>(configuration.Value) is { } read
            ? read with { Signal = read.Signal ?? WorkflowContractReference.Any }
            : null;
}
