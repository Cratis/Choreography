// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// A join gathered the branches of a for-each.
/// </summary>
/// <param name="Join">The <see cref="NodeExecutionKey"/> of the join.</param>
/// <param name="Outlet">The <see cref="OutletName"/> it left through.</param>
/// <param name="Results">Each branch's result - status, outlet and output - keyed by branch, as <see cref="WorkflowData"/>.</param>
public record WorkflowBranchesJoined(NodeExecutionKey Join, OutletName Outlet, WorkflowData Results);
