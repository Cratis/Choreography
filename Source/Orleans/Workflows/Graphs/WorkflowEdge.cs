// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Graphs;

/// <summary>
/// A connection from one node's outlet to another node.
/// </summary>
/// <param name="From">The <see cref="WorkflowNodeId"/> the edge leaves.</param>
/// <param name="Outlet">The <see cref="OutletName"/> of the node it leaves through.</param>
/// <param name="To">The <see cref="WorkflowNodeId"/> the edge enters.</param>
public record WorkflowEdge(WorkflowNodeId From, OutletName Outlet, WorkflowNodeId To);
