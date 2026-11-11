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

        violations.AddRange(CheckDuplicates(nodes));
        violations.AddRange(CheckTriggers(nodes));
        violations.AddRange(CheckEdges(graph, nodes));
        violations.AddRange(CheckConfigurations(nodes));
        violations.AddRange(CheckForEaches(graph));
        violations.AddRange(CheckCycles(graph, nodes));
        return violations;
    }

    static IEnumerable<WorkflowStructureViolation> CheckDuplicates(List<WorkflowNode> nodes) => nodes
        .GroupBy(node => node.Id)
        .Where(group => group.Count() > 1)
        .Select(group => new WorkflowStructureViolation(WorkflowStructureViolationKind.DuplicateNode, group.Key, $"More than one node is called '{group.Key}'"));

    static IEnumerable<WorkflowStructureViolation> CheckTriggers(List<WorkflowNode> nodes)
    {
        var triggers = nodes.Where(node => node.Kind == WorkflowNodeKind.Trigger).ToList();
        if (triggers.Count == 0)
        {
            yield return new(WorkflowStructureViolationKind.NoTrigger, string.Empty, "The workflow has no trigger");
        }

        foreach (var node in triggers.Skip(1))
        {
            yield return new(WorkflowStructureViolationKind.MoreThanOneTrigger, node.Id, "The workflow has more than one trigger");
        }
    }

    static IEnumerable<WorkflowStructureViolation> CheckEdges(WorkflowGraph graph, List<WorkflowNode> nodes)
    {
        var byId = nodes.DistinctBy(node => node.Id).ToDictionary(node => node.Id);
        foreach (var edge in graph.Edges)
        {
            foreach (var violation in CheckEdge(edge, byId))
            {
                yield return violation;
            }
        }
    }

    static IEnumerable<WorkflowStructureViolation> CheckEdge(WorkflowEdge edge, Dictionary<WorkflowNodeId, WorkflowNode> byId)
    {
        if (!byId.TryGetValue(edge.From, out var from))
        {
            yield return new(WorkflowStructureViolationKind.UnknownNode, edge.From, $"An edge leaves '{edge.From}', which is not a node");
            yield break;
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

    static IEnumerable<WorkflowStructureViolation> CheckConfigurations(List<WorkflowNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!HasValidConfiguration(node))
            {
                yield return new(WorkflowStructureViolationKind.InvalidConfiguration, node.Id, $"The configuration of '{node.Id}' does not describe a {node.Kind}");
            }
        }
    }

    static bool HasValidConfiguration(WorkflowNode node) => node.Kind switch
    {
        WorkflowNodeKind.ForEach => ForEachConfiguration.From(node.Configuration) is not null,
        WorkflowNodeKind.Join => JoinConfiguration.From(node.Configuration) is not null,
        WorkflowNodeKind.Wait => WaitConfiguration.From(node.Configuration) is not null,
        _ => true
    };

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

    static List<List<WorkflowNodeId>> StronglyConnected(WorkflowGraph graph, IEnumerable<WorkflowNodeId> nodes) => new ComponentFinder(graph, nodes).Find();

    sealed class ComponentFinder
    {
        readonly Dictionary<WorkflowNodeId, int> _indices = [];
        readonly Dictionary<WorkflowNodeId, int> _lowLinks = [];
        readonly Stack<WorkflowNodeId> _stack = new();
        readonly HashSet<WorkflowNodeId> _onStack = [];
        readonly List<List<WorkflowNodeId>> _components = [];
        readonly ILookup<WorkflowNodeId, WorkflowNodeId> _successors;
        readonly HashSet<WorkflowNodeId> _known;
        int _index;

        internal ComponentFinder(WorkflowGraph graph, IEnumerable<WorkflowNodeId> nodes)
        {
            _successors = graph.Edges.ToLookup(edge => edge.From, edge => edge.To);
            _known = nodes.ToHashSet();
        }

        internal List<List<WorkflowNodeId>> Find()
        {
            foreach (var node in _known)
            {
                if (!_indices.ContainsKey(node))
                {
                    Visit(node);
                }
            }

            return _components;
        }

        void Visit(WorkflowNodeId node)
        {
            _indices[node] = _lowLinks[node] = _index++;
            _stack.Push(node);
            _onStack.Add(node);
            foreach (var next in _successors[node].Where(_known.Contains).Distinct().ToList())
            {
                if (_indices.TryGetValue(next, out var nextIndex))
                {
                    if (_onStack.Contains(next))
                    {
                        _lowLinks[node] = Math.Min(_lowLinks[node], nextIndex);
                    }

                    continue;
                }

                Visit(next);
                _lowLinks[node] = Math.Min(_lowLinks[node], _lowLinks[next]);
            }

            if (_lowLinks[node] != _indices[node])
            {
                return;
            }

            PopComponent(node);
        }

        void PopComponent(WorkflowNodeId node)
        {
            var component = new List<WorkflowNodeId>();
            WorkflowNodeId member;
            do
            {
                member = _stack.Pop();
                _onStack.Remove(member);
                component.Add(member);
            }
            while (member != node);
            _components.Add(component);
        }
    }
}
