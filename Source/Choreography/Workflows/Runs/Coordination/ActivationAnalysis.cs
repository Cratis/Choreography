// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// Derives which node executions a run has activated from the routes its history recorded.
/// </summary>
/// <remarks>
/// Activation is never stored. The trigger is always activated; every recorded route then activates the
/// nodes its outlet leads to, in the branch it leads into, numbering visits in recorded order. An edge
/// into a join does not activate the join - it marks the branch as having arrived; a fan-out activates
/// its join once, in the for-each's own branch.
/// </remarks>
public class ActivationAnalysis
{
    readonly List<NodeExecutionKey> _activations = [];
    readonly HashSet<NodeExecutionKey> _activated = [];
    readonly Dictionary<WorkflowBranchKey, NodeExecutionKey> _arrivals = [];
    readonly Dictionary<NodeExecutionKey, NodeExecutionKey> _joinOfForEach = [];
    readonly Dictionary<NodeExecutionKey, NodeExecutionKey> _forEachOfJoin = [];
    readonly Dictionary<(WorkflowNodeId Node, WorkflowBranchKey Branch), int> _visits = [];

    ActivationAnalysis()
    {
    }

    /// <summary>
    /// Gets the activated executions, in activation order.
    /// </summary>
    public IReadOnlyList<NodeExecutionKey> Activations => _activations;

    /// <summary>
    /// Analyzes a run.
    /// </summary>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <returns>The <see cref="ActivationAnalysis"/>.</returns>
    public static ActivationAnalysis Of(WorkflowRunState state)
    {
        var analysis = new ActivationAnalysis();
        if (!state.IsAdmitted)
        {
            return analysis;
        }

        var graph = state.Graph;
        var trigger = graph.Nodes.FirstOrDefault(node => node.Kind == WorkflowNodeKind.Trigger);
        if (trigger is not null)
        {
            analysis.Activate(trigger.Id, WorkflowBranchKey.Root);
        }

        var scopes = ForEachScope.In(graph);
        foreach (var route in state.Routes)
        {
            if (route.ActivatesJoin)
            {
                if (scopes.TryGetValue(route.From.Node, out var scope) && scope.Join is { } join)
                {
                    var joinKey = analysis.Activate(join, route.Into);
                    analysis._joinOfForEach[route.From] = joinKey;
                    analysis._forEachOfJoin[joinKey] = route.From;
                }

                continue;
            }

            analysis.Follow(graph, route);
        }

        return analysis;
    }

    /// <summary>
    /// Gets whether an execution was activated.
    /// </summary>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <returns>True if it was.</returns>
    public bool IsActivated(NodeExecutionKey execution) => _activated.Contains(execution);

    /// <summary>
    /// Gets the execution a branch arrived at its join through, if it has.
    /// </summary>
    /// <param name="branch">The <see cref="WorkflowBranchKey"/>.</param>
    /// <returns>The <see cref="NodeExecutionKey"/> that arrived, or null.</returns>
    public NodeExecutionKey? ArrivalOf(WorkflowBranchKey branch) => _arrivals.TryGetValue(branch, out var arrival) ? arrival : null;

    /// <summary>
    /// Gets the join execution a fan-out activated.
    /// </summary>
    /// <param name="forEach">The <see cref="NodeExecutionKey"/> of the for-each.</param>
    /// <returns>The join's <see cref="NodeExecutionKey"/>, or null.</returns>
    public NodeExecutionKey? JoinOf(NodeExecutionKey forEach) => _joinOfForEach.TryGetValue(forEach, out var join) ? join : null;

    /// <summary>
    /// Gets the for-each execution whose fan-out activated a join execution.
    /// </summary>
    /// <param name="join">The <see cref="NodeExecutionKey"/> of the join.</param>
    /// <returns>The for-each's <see cref="NodeExecutionKey"/>, or null.</returns>
    public NodeExecutionKey? ForEachOf(NodeExecutionKey join) => _forEachOfJoin.TryGetValue(join, out var forEach) ? forEach : null;

    void Follow(WorkflowGraph graph, WorkflowRoute route)
    {
        foreach (var target in graph.TargetsOf(route.From.Node, route.Outlet))
        {
            if (graph.NodeFor(target)?.Kind == WorkflowNodeKind.Join)
            {
                if (route.Into != WorkflowBranchKey.Root)
                {
                    _arrivals.TryAdd(route.Into, route.From);
                }

                continue;
            }

            Activate(target, route.Into);
        }
    }

    NodeExecutionKey Activate(WorkflowNodeId node, WorkflowBranchKey branch)
    {
        var visit = _visits.TryGetValue((node, branch), out var count) ? count + 1 : 1;
        _visits[(node, branch)] = visit;
        var key = new NodeExecutionKey(node, branch, visit);
        _activations.Add(key);
        _activated.Add(key);
        return key;
    }
}
