// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// Finishes a run once nothing in it is in flight - recording what was never reached, then how it ended.
/// </summary>
/// <remarks>
/// A run is quiescent when no execution is scheduled, waiting for a retry, or waiting. What was
/// activated but never taken forward - because an unhandled failure stopped planning, or a join had
/// already gathered its branch - and every node nothing led to, in every branch it could have run in,
/// is recorded as unreachable. The run then needs reconciliation if any outcome is unknown, has failed
/// if a failure in the root branch went unhandled, and has succeeded otherwise.
/// </remarks>
public static class ReachabilityAnalysis
{
    /// <summary>
    /// Finishes the run if it is quiescent.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    public static void FinishIfQuiescent(CoordinatorWork work)
    {
        var state = work.State;
        if (!state.IsAdmitted || state.IsOver || state.Executions.Values.Any(execution => execution.IsInFlight))
        {
            return;
        }

        var unreachable = Unreachable(state).ToList();
        if (unreachable.Count > 0)
        {
            work.Record(new WorkflowNodesUnreachable(unreachable));
        }

        if (state.Executions.Values.Any(execution => execution.Status == NodeExecutionStatus.ReconciliationRequired))
        {
            work.Record(new WorkflowRunNeedsReconciliation());
        }
        else if (state.UnroutedFailure is { } failure)
        {
            work.Record(new WorkflowRunFailed(failure));
        }
        else
        {
            work.Record(new WorkflowRunSucceeded());
        }
    }

    static IEnumerable<NodeExecutionKey> Unreachable(WorkflowRunState state)
    {
        var analysis = ActivationAnalysis.Of(state);
        var graph = state.Graph;
        var scopes = ForEachScope.In(graph);
        var bodies = scopes.Values.SelectMany(scope => scope.Body).ToHashSet();
        var activatedNodes = analysis.Activations.Select(activation => (activation.Node, activation.Branch)).ToHashSet();

        var neverTakenForward = analysis.Activations.Where(activation => !state.Executions.ContainsKey(activation));

        var neverReachedInRoot = graph.Nodes
            .Select(node => node.Id)
            .Where(node => !activatedNodes.Contains((node, WorkflowBranchKey.Root)) && (!bodies.Contains(node) || !BodyRanAnywhere(state, scopes, node)))
            .Select(NodeExecutionKey.RootOf);

        var neverReachedInBranches = state.Branches
            .SelectMany(branch => scopes.TryGetValue(branch.Value.ForEach.Node, out var scope)
                ? scope.Body.Where(node => !activatedNodes.Contains((node, branch.Key))).Select(node => new NodeExecutionKey(node, branch.Key, 1))
                : []);

        return neverTakenForward
            .Concat(neverReachedInRoot)
            .Concat(neverReachedInBranches)
            .Where(execution => !state.Executions.ContainsKey(execution))
            .Distinct();
    }

    static bool BodyRanAnywhere(WorkflowRunState state, IReadOnlyDictionary<WorkflowNodeId, ForEachScope> scopes, WorkflowNodeId node) =>
        state.Branches.Values.Any(origin => scopes.TryGetValue(origin.ForEach.Node, out var scope) && scope.Body.Contains(node));
}
