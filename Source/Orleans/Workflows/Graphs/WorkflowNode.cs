// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Graphs;

/// <summary>
/// One node of a workflow graph.
/// </summary>
/// <param name="Id">The <see cref="WorkflowNodeId"/>.</param>
/// <param name="Kind">The <see cref="WorkflowNodeKind"/>.</param>
/// <param name="Capability">The <see cref="CapabilityReference"/> that performs the node.</param>
/// <param name="Configuration">The node's <see cref="WorkflowNodeConfiguration"/>.</param>
/// <param name="Outlets">The outlets the node may leave through - pinned when the node was authored.</param>
/// <param name="Policy">The node's <see cref="WorkflowNodePolicy"/>.</param>
public record WorkflowNode(
    WorkflowNodeId Id,
    WorkflowNodeKind Kind,
    CapabilityReference Capability,
    WorkflowNodeConfiguration Configuration,
    IEnumerable<OutletName> Outlets,
    WorkflowNodePolicy Policy)
{
    /// <summary>
    /// Gets whether the node declares an outlet.
    /// </summary>
    /// <param name="outlet">The <see cref="OutletName"/> to look for.</param>
    /// <returns>True if the node declares it.</returns>
    public bool Declares(OutletName outlet) => Outlets.Contains(outlet);
}
