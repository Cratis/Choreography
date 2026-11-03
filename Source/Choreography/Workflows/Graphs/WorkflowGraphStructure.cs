// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// Checks that a workflow graph is structurally something the engine can run.
/// </summary>
/// <remarks>
/// Structure only - one trigger, edges that refer to real nodes and declared outlets, a single join per
/// for-each, no nested for-each, and no cycle through a node that may only be visited once. Whether the
/// capabilities exist, fit together or may be used here is the catalog's question, not this one.
/// </remarks>
public static class WorkflowGraphStructure
{
    /// <summary>
    /// Checks a graph.
    /// </summary>
    /// <param name="graph">The <see cref="WorkflowGraph"/> to check.</param>
    /// <returns>Every structural violation - empty when the graph can run.</returns>
    public static IEnumerable<WorkflowStructureViolation> Check(WorkflowGraph graph)
    {
        var nodes = graph.Nodes.ToList();
        var violations = new List<WorkflowStructureViolation>();

        violations.AddRange(nodes
            .GroupBy(node => node.Id)
            .Where(group => group.Count() > 1)
            .Select(group => new WorkflowStructureViolation(WorkflowStructureViolationKind.DuplicateNode, group.Key, $"More than one node is called '{group.Key}'")));

        var triggers = nodes.Where(node => node.Kind == WorkflowNodeKind.Trigger).ToList();
        if (triggers.Count == 0)
        {
            violations.Add(new(WorkflowStructureViolationKind.NoTrigger, string.Empty, "The workflow has no trigger"));
        }

        violations.AddRange(triggers.Skip(1).Select(node => new WorkflowStructureViolation(WorkflowStructureViolationKind.MoreThanOneTrigger, node.Id, "The workflow has more than one trigger")));
        violations.AddRange(CheckEdges(graph, nodes));
        violations.AddRange(CheckConfigurations(nodes));
        violations.AddRange(CheckForEaches(graph));
        violations.AddRange(CheckCycles(graph, nodes));
        return violations;
    }

    static IEnumerable<WorkflowStructureViolation> CheckEdges(WorkflowGraph graph, List<WorkflowNode> nodes)
    {
        var byId = nodes.DistinctBy(node => node.Id).ToDictionary(node => node.Id);
        foreach (var edge in graph.Edges)
        {
            if (!byId.TryGetValue(edge.From, out var from))
            {
                yield return new(WorkflowStructureViolationKind.UnknownNode, edge.From, $"An edge leaves '{edge.From}', which is not a node");
                continue;
            }

            if (!byId.ContainsKey(edge.To))
            {
                yield return new(WorkflowStructureViolationKind.UnknownNode, edge.To, $"An edge enters '{edge.To}', which is not a node");
            }

            if (!from.Declares(edge.Outlet))
            {
                yield return new(WorkflowStructureViolationKind.UndeclaredOutlet, edge.From, $"'{edge.From}' has no outlet called '{edge.Outlet}'");
            }
        }
    }

    static IEnumerable<WorkflowStructureViolation> CheckConfigurations(List<WorkflowNode> nodes)
    {
        foreach (var node in nodes)
        {
            var valid = node.Kind switch
            {
                WorkflowNodeKind.ForEach => ForEachConfiguration.From(node.Configuration) is not null,
                WorkflowNodeKind.Join => JoinConfiguration.From(node.Configuration) is not null,
                WorkflowNodeKind.Wait => WaitConfiguration.From(node.Configuration) is not null,
                _ => true
            };

            if (!valid)
            {
                yield return new(WorkflowStructureViolationKind.InvalidConfiguration, node.Id, $"The configuration of '{node.Id}' does not describe a {node.Kind}");
            }
        }
    }

    static IEnumerable<WorkflowStructureViolation> CheckForEaches(WorkflowGraph graph)
    {
        var scopes = ForEachScope.In(graph);
        foreach (var scope in scopes.Values)
        {
            if (scope.Joins.Count != 1)
            {
                yield return new(WorkflowStructureViolationKind.ForEachWithoutSingleJoin, scope.ForEach, $"'{scope.ForEach}' needs exactly one join, it has {scope.Joins.Count}");
            }

            foreach (var nested in scope.Body.Where(scopes.ContainsKey))
            {
                yield return new(WorkflowStructureViolationKind.NestedForEachNotSupported, nested, $"'{nested}' is a for-each inside the branches of '{scope.ForEach}'");
            }
        }
    }

    static IEnumerable<WorkflowStructureViolation> CheckCycles(WorkflowGraph graph, List<WorkflowNode> nodes)
    {
        var byId = nodes.DistinctBy(node => node.Id).ToDictionary(node => node.Id);
        return StronglyConnected(graph, byId.Keys)
            .Where(component => component.Count > 1 || graph.Edges.Any(edge => edge.From == component[0] && edge.To == component[0]))
            .SelectMany(component => component)
            .Where(node => byId[node].Policy.MaxVisits <= 1)
            .Select(node => new WorkflowStructureViolation(WorkflowStructureViolationKind.UnboundedCycle, node, $"'{node}' is on a cycle but may only be visited once"));
    }

    static List<List<WorkflowNodeId>> StronglyConnected(WorkflowGraph graph, IEnumerable<WorkflowNodeId> nodes)
    {
        var index = 0;
        var indices = new Dictionary<WorkflowNodeId, int>();
        var lowLinks = new Dictionary<WorkflowNodeId, int>();
        var stack = new Stack<WorkflowNodeId>();
        var onStack = new HashSet<WorkflowNodeId>();
        var components = new List<List<WorkflowNodeId>>();
        var successors = graph.Edges.ToLookup(edge => edge.From, edge => edge.To);
        var known = nodes.ToHashSet();

        void Visit(WorkflowNodeId node)
        {
            indices[node] = lowLinks[node] = index++;
            stack.Push(node);
            onStack.Add(node);
            foreach (var next in successors[node].Where(known.Contains).Distinct().ToList())
            {
                if (indices.TryGetValue(next, out var nextIndex))
                {
                    if (onStack.Contains(next))
                    {
                        lowLinks[node] = Math.Min(lowLinks[node], nextIndex);
                    }

                    continue;
                }

                Visit(next);
                lowLinks[node] = Math.Min(lowLinks[node], lowLinks[next]);
            }

            if (lowLinks[node] != indices[node])
            {
                return;
            }

            var component = new List<WorkflowNodeId>();
            WorkflowNodeId member;
            do
            {
                member = stack.Pop();
                onStack.Remove(member);
                component.Add(member);
            }
            while (member != node);
            components.Add(component);
        }

        foreach (var node in known)
        {
            if (!indices.ContainsKey(node))
            {
                Visit(node);
            }
        }

        return components;
    }
}
