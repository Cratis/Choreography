// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// Fans a for-each out into one branch per item, and holds its branches to their parallelism.
/// </summary>
/// <remarks>
/// The items are read from the configured property of the configured node's output. Every item must be
/// an object with a unique string <c lang="csharp">key</c> - the key names the branch, so a missing or repeated key
/// fails the for-each with <see cref="WorkflowFailureKind.InvalidOutput"/> rather than guessing.
/// </remarks>
public static class ForEachExpansion
{
    /// <summary>
    /// Fans a for-each out.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    /// <param name="node">The for-each <see cref="WorkflowNode"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/> of the for-each.</param>
    public static void FanOut(CoordinatorWork work, WorkflowNode node, NodeExecutionKey execution)
    {
        var configuration = ForEachConfiguration.From(node.Configuration);
        var output = configuration is null ? null : NodeInputComposer.LatestOutput(work.State, configuration.ItemsFrom, execution.Branch);
        IReadOnlyList<WorkflowBranch> branches = [];
        string? problem;
        if (configuration is null)
        {
            problem = $"The configuration of '{node.Id}' does not describe a for-each";
        }
        else if (output is null)
        {
            problem = $"'{configuration.ItemsFrom}' has no output for '{node.Id}' to read items from";
        }
        else
        {
            (branches, problem) = BranchesFrom(execution, output, configuration.ItemsProperty);
        }

        if (problem is not null)
        {
            var reason = new WorkflowFailureReason(problem);
            work.Record(new WorkflowNodeAttemptFailed(execution, WorkflowAttempt.First, WorkflowFailureKind.InvalidOutput, reason));
            work.Record(new WorkflowNodeFailed(execution, WorkflowAttempt.First, WorkflowFailureKind.InvalidOutput, reason, FailureRouting.For(work.State.Graph, node.Id, WorkflowFailureKind.InvalidOutput)));
            return;
        }

        work.Record(new WorkflowBranchesFannedOut(execution, branches));
    }

    /// <summary>
    /// Gets whether an execution may be scheduled without exceeding its for-each's parallelism.
    /// </summary>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/> to schedule.</param>
    /// <returns>True if it may - always outside a for-each.</returns>
    public static bool HasCapacityFor(WorkflowRunState state, NodeExecutionKey execution)
    {
        if (!state.Branches.TryGetValue(execution.Branch, out var origin)
            || ConfigurationOf(state, origin) is not { } configuration)
        {
            return true;
        }

        var inFlight = state.Executions.Values.Count(candidate => IsInFlightFor(state, candidate, origin));

        return inFlight < configuration.MaxParallelism;
    }

    static ForEachConfiguration? ConfigurationOf(WorkflowRunState state, WorkflowBranchOrigin origin) =>
        state.Graph.NodeFor(origin.ForEach.Node) is { } forEach ? ForEachConfiguration.From(forEach.Configuration) : null;

    static bool IsInFlightFor(WorkflowRunState state, NodeExecution candidate, WorkflowBranchOrigin origin) =>
        candidate.Status is NodeExecutionStatus.Scheduled or NodeExecutionStatus.RetryPending
        && state.Branches.TryGetValue(candidate.Key.Branch, out var candidateOrigin)
        && candidateOrigin.ForEach == origin.ForEach;

    static string? KeyOf(JsonObject item) =>
        item["key"] is JsonValue keyValue && keyValue.TryGetValue<string>(out var key) && !string.IsNullOrEmpty(key) ? key : null;

    static (IReadOnlyList<WorkflowBranch> Branches, string? Problem) BranchesFrom(NodeExecutionKey forEach, WorkflowData output, string property)
    {
        JsonNode? items;
        try
        {
            items = JsonNode.Parse(output.Value)?[property];
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            return ([], $"The output holding the items of '{forEach}' is not a JSON object");
        }

        if (items is not JsonArray array)
        {
            return ([], $"'{property}' is not a list of items for '{forEach}'");
        }

        List<WorkflowBranch> branches = [with(capacity: array.Count)];
        HashSet<string> keys = [with(StringComparer.Ordinal)];
        foreach (var item in array)
        {
            if (item is not JsonObject itemObject || KeyOf(itemObject) is not { } key)
            {
                return ([], $"Every item of '{forEach}' must be an object with a string 'key'");
            }

            if (!keys.Add(key))
            {
                return ([], $"The item key '{key}' appears more than once for '{forEach}'");
            }

            branches.Add(new($"{forEach}:{Uri.EscapeDataString(key)}", itemObject.ToJsonString()));
        }

        return (branches, null);
    }
}
