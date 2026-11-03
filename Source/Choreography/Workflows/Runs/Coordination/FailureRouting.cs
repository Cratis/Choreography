// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// Decides which outlet a failed node leaves through.
/// </summary>
/// <remarks>
/// A node that answered with something it did not declare - an outlet or an output - leaves through
/// <c lang="csharp">fallback</c> when that is connected. Any failure then leaves through <c lang="csharp">failed</c> when that is
/// connected. Otherwise the failure is unrouted, and nothing handles it.
/// </remarks>
public static class FailureRouting
{
    /// <summary>
    /// Gets the outlet a failure is routed through.
    /// </summary>
    /// <param name="graph">The <see cref="WorkflowGraph"/>.</param>
    /// <param name="node">The <see cref="WorkflowNodeId"/> that failed.</param>
    /// <param name="kind">The <see cref="WorkflowFailureKind"/>.</param>
    /// <returns>The <see cref="OutletName"/>, or <see cref="OutletName.None"/> when nothing handles it.</returns>
    public static OutletName For(WorkflowGraph graph, WorkflowNodeId node, WorkflowFailureKind kind)
    {
        if (kind == WorkflowFailureKind.VisitLimitExceeded)
        {
            return OutletName.None;
        }

        if (kind is WorkflowFailureKind.InvalidOutlet or WorkflowFailureKind.InvalidOutput && graph.IsConnected(node, WellKnownOutlets.Fallback))
        {
            return WellKnownOutlets.Fallback;
        }

        return graph.IsConnected(node, WellKnownOutlets.Failed) ? WellKnownOutlets.Failed : OutletName.None;
    }
}
