// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Graphs;

/// <summary>
/// A workflow's graph - its nodes and the edges between their outlets.
/// </summary>
/// <param name="Nodes">The <see cref="WorkflowNode">nodes</see>.</param>
/// <param name="Edges">The <see cref="WorkflowEdge">edges</see>.</param>
/// <remarks>
/// Every run records the graph it was admitted with, forever - so this shape stays minimal, and what
/// varies per adapter lives in each node's configuration text instead.
/// </remarks>
public record WorkflowGraph(IEnumerable<WorkflowNode> Nodes, IEnumerable<WorkflowEdge> Edges)
{
    /// <summary>
    /// Gets a node by its identity.
    /// </summary>
    /// <param name="node">The <see cref="WorkflowNodeId"/>.</param>
    /// <returns>The <see cref="WorkflowNode"/>, or null when the graph has no such node.</returns>
    public WorkflowNode? NodeFor(WorkflowNodeId node) => Nodes.FirstOrDefault(candidate => candidate.Id == node);

    /// <summary>
    /// Gets the nodes an outlet of a node leads to.
    /// </summary>
    /// <param name="node">The <see cref="WorkflowNodeId"/> the edges leave.</param>
    /// <param name="outlet">The <see cref="OutletName"/> they leave through.</param>
    /// <returns>The targets, in edge order.</returns>
    public IEnumerable<WorkflowNodeId> TargetsOf(WorkflowNodeId node, OutletName outlet) =>
        Edges.Where(edge => edge.From == node && edge.Outlet == outlet).Select(edge => edge.To);

    /// <summary>
    /// Gets whether an outlet of a node is connected to anything.
    /// </summary>
    /// <param name="node">The <see cref="WorkflowNodeId"/>.</param>
    /// <param name="outlet">The <see cref="OutletName"/>.</param>
    /// <returns>True if at least one edge leaves through it.</returns>
    public bool IsConnected(WorkflowNodeId node, OutletName outlet) => TargetsOf(node, outlet).Any();
}
