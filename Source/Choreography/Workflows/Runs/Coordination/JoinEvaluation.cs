// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.Coordination;

/// <summary>
/// Gathers the branches of a for-each at its join.
/// </summary>
/// <remarks>
/// A branch has succeeded only after all activated work has ended without failure or ambiguity and
/// at least one path arrived at the join (or work was skipped); it has ended otherwise once
/// nothing in it is in flight. The join fires as soon as its mode is satisfied, or once every branch has
/// ended: it leaves through its completed outlet when its mode was satisfied, and its partial outlet
/// when it was not. Its output is every branch's result - status, outlet and output - keyed by branch.
/// </remarks>
public static class JoinEvaluation
{
    /// <summary>The status of a branch that arrived at the join.</summary>
    public const string Succeeded = "succeeded";

    /// <summary>The status of a branch whose work was skipped.</summary>
    public const string Skipped = "skipped";

    /// <summary>The status of a branch where something failed.</summary>
    public const string Failed = "failed";

    /// <summary>The status of a branch whose outcome somebody has to establish.</summary>
    public const string ReconciliationRequired = "reconciliationRequired";

    /// <summary>The status of a branch that ended without arriving.</summary>
    public const string Ended = "ended";

    /// <summary>The status of a branch that is still going.</summary>
    public const string Running = "running";

    /// <summary>
    /// Joins a for-each's branches, if the join can fire.
    /// </summary>
    /// <param name="work">The <see cref="CoordinatorWork"/>.</param>
    /// <param name="analysis">The run's <see cref="ActivationAnalysis"/>.</param>
    /// <param name="node">The join <see cref="WorkflowNode"/>.</param>
    /// <param name="execution">The join's <see cref="NodeExecutionKey"/>.</param>
    /// <returns>True if the join fired.</returns>
    public static bool TryJoin(CoordinatorWork work, ActivationAnalysis analysis, WorkflowNode node, NodeExecutionKey execution)
    {
        var state = work.State;
        if (analysis.ForEachOf(execution) is not { } forEach || !state.FanOuts.TryGetValue(forEach, out var fanOut))
        {
            return false;
        }

        var configuration = JoinConfiguration.From(node.Configuration) ?? new(forEach.Node, JoinMode.All, 0);
        var results = fanOut.Branches.Select(branch => (branch.Key, Result: ResultOf(state, analysis, branch.Key))).ToList();
        var succeeded = results.Count(result => CountsAsSucceeded(result.Result.Status));
        var satisfied = configuration.Mode switch
        {
            JoinMode.Any => succeeded >= 1,
            JoinMode.AtLeast => succeeded >= configuration.Threshold,
            _ => succeeded == results.Count
        };

        if (!satisfied && results.Exists(result => string.Equals(result.Result.Status, Running, StringComparison.Ordinal)))
        {
            return false;
        }

        var output = new JsonObject();
        foreach (var (key, result) in results)
        {
            output[key.Value] = new JsonObject
            {
                ["key"] = JsonNode.Parse(state.Branches[key].Item.Value)?["key"]?.DeepClone(),
                ["status"] = result.Status,
                ["outlet"] = result.Outlet,
                ["output"] = result.Output
            };
        }

        var data = new WorkflowData(output.ToJsonString());
        if (data.ExceedsLimit || !data.IsValidJson)
        {
            OutcomeRecording.RejectData(work, execution, WorkflowAttempt.First, "The joined output exceeds the bounded JSON contract");
            return true;
        }

        work.Record(new WorkflowBranchesJoined(execution, satisfied ? WellKnownOutlets.Completed : WellKnownOutlets.Partial, data));
        return true;
    }

    static (string Status, string Outlet, JsonNode? Output) ResultOf(WorkflowRunState state, ActivationAnalysis analysis, WorkflowBranchKey branch)
    {
        var executions = state.Executions.Values.Where(execution => execution.Key.Branch == branch).ToList();
        var pending = analysis.Activations.Any(activation => activation.Branch == branch && !state.Executions.ContainsKey(activation));
        if (pending || executions.Exists(execution => execution.IsInFlight))
        {
            return (Running, string.Empty, null);
        }

        if (!executions.Exists(execution => execution.Status is NodeExecutionStatus.Failed or NodeExecutionStatus.ReconciliationRequired)
            && analysis.ArrivalOf(branch) is { } arrival && state.Executions.TryGetValue(arrival, out var arrived))
        {
            return (Succeeded, arrived.Outlet.Value, ParseOrNull(arrived.Output.Value));
        }

        var last = executions.LastOrDefault(execution => execution.Status != NodeExecutionStatus.Succeeded) ?? executions.LastOrDefault();
        return (StatusOf(executions), last?.Outlet.Value ?? string.Empty, last is null ? null : ParseOrNull(last.Output.Value));
    }

    static string StatusOf(List<NodeExecution> executions)
    {
        if (executions.Exists(execution => execution.Status == NodeExecutionStatus.Failed))
        {
            return Failed;
        }

        if (executions.Exists(execution => execution.Status == NodeExecutionStatus.ReconciliationRequired))
        {
            return ReconciliationRequired;
        }

        return executions.Exists(execution => execution.Status == NodeExecutionStatus.Skipped) ? Skipped : Ended;
    }

    static bool CountsAsSucceeded(string status) =>
        string.Equals(status, Succeeded, StringComparison.Ordinal) || string.Equals(status, Skipped, StringComparison.Ordinal);

    static JsonNode? ParseOrNull(string json)
    {
        try
        {
            return JsonNode.Parse(json);
        }
        catch (System.Text.Json.JsonException)
        {
            return JsonValue.Create(json);
        }
    }
}
