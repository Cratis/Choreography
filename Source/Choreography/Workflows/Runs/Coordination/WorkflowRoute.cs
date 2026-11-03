// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// One way out of a node execution the run's history recorded, in the order it was recorded.
/// </summary>
/// <param name="From">The <see cref="NodeExecutionKey"/> it leaves.</param>
/// <param name="Outlet">The <see cref="OutletName"/> it leaves through.</param>
/// <param name="Into">The <see cref="WorkflowBranchKey"/> the nodes it leads to run in.</param>
/// <param name="ActivatesJoin">Whether the route activates the join of the for-each it leaves, rather than following edges.</param>
/// <remarks>
/// Which node executions a run has activated is never stored: it is derived from these routes, in order,
/// whenever it is needed - so it can only ever agree with the history.
/// </remarks>
public record WorkflowRoute(NodeExecutionKey From, OutletName Outlet, WorkflowBranchKey Into, bool ActivatesJoin = false);
