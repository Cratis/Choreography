// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// The part of a graph one for-each fans out over - the nodes its branches run, and the join that gathers them.
/// </summary>
/// <param name="ForEach">The for-each <see cref="WorkflowNodeId"/>.</param>
/// <param name="Joins">The join nodes configured for the for-each - exactly one in a well-formed graph.</param>
/// <param name="Body">The nodes reachable from the for-each's <c lang="csharp">each</c> outlet before its join.</param>
public record ForEachScope(WorkflowNodeId ForEach, IReadOnlyList<WorkflowNodeId> Joins, IReadOnlySet<WorkflowNodeId> Body)
{
    /// <summary>
    /// Gets the join of the for-each, when it has exactly one.
    /// </summary>
    public WorkflowNodeId? Join => Joins.Count == 1 ? Joins[0] : null;

    /// <summary>
    /// Finds the scope of every for-each in a graph.
    /// </summary>
    /// <param name="graph">The <see cref="WorkflowGraph"/>.</param>
    /// <returns>The scopes, keyed by for-each.</returns>
    public static IReadOnlyDictionary<WorkflowNodeId, ForEachScope> In(WorkflowGraph graph)
    {
        var joins = graph.Nodes
            .Where(node => node.Kind == WorkflowNodeKind.Join)
            .Select(node => (node.Id, Configuration: JoinConfiguration.From(node.Configuration)))
            .Where(join => join.Configuration is not null)
            .ToLookup(join => join.Configuration!.ForEach, join => join.Id);

        return graph.Nodes
            .Where(node => node.Kind == WorkflowNodeKind.ForEach)
            .DistinctBy(node => node.Id)
            .ToDictionary(
                node => node.Id,
                node =>
                {
                    var ofThis = joins[node.Id].ToList();
                    return new ForEachScope(node.Id, ofThis, BodyOf(graph, node.Id, [.. ofThis]));
                });
    }

    static HashSet<WorkflowNodeId> BodyOf(WorkflowGraph graph, WorkflowNodeId forEach, HashSet<WorkflowNodeId> joins)
    {
        var body = new HashSet<WorkflowNodeId>();
        var pending = new Queue<WorkflowNodeId>(graph.TargetsOf(forEach, WellKnownOutlets.Each));
        while (pending.TryDequeue(out var node))
        {
            if (joins.Contains(node) || node == forEach || !body.Add(node))
            {
                continue;
            }

            foreach (var next in graph.Edges.Where(edge => edge.From == node).Select(edge => edge.To))
            {
                pending.Enqueue(next);
            }
        }

        return body;
    }
}
