// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination;

/// <summary>
/// Composes the input a node execution is given, from what the run has recorded.
/// </summary>
/// <remarks>
/// The input is <c lang="csharp">{ "trigger": { payload, subject, correlation }, "nodes": { id: output }, "item": … }</c>.
/// A node's output is its latest successful visit in the same branch, or failing that, in the root
/// branch; <c lang="csharp">item</c> is the branch's for-each item, or an empty object outside a for-each.
/// </remarks>
public static class NodeInputComposer
{
    /// <summary>
    /// Composes the input of an execution.
    /// </summary>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <returns>The input, as <see cref="WorkflowData"/>.</returns>
    public static WorkflowData For(WorkflowRunState state, NodeExecutionKey execution)
    {
        var trigger = state.Admission!.Trigger;
        if (!trigger.Payload.IsValidJson)
        {
            return trigger.Payload;
        }

        var nodes = new JsonObject();
        foreach (var node in state.Executions.Values.Select(candidate => candidate.Key.Node).Distinct())
        {
            if (LatestOutput(state, node, execution.Branch) is { } output)
            {
                if (!output.IsValidJson)
                {
                    return output;
                }

                nodes[node.Value] = Parse(output);
            }
        }

        var input = new JsonObject
        {
            ["trigger"] = new JsonObject
            {
                ["payload"] = Parse(trigger.Payload),
                ["subject"] = new JsonObject { ["kind"] = trigger.Subject.Kind.Value, ["id"] = trigger.Subject.Id.Value },
                ["correlation"] = trigger.Correlation.Value
            },
            ["nodes"] = nodes,
            ["item"] = state.Branches.TryGetValue(execution.Branch, out var origin) ? Parse(origin.Item) : new JsonObject()
        };

        return new(input.ToJsonString());
    }

    /// <summary>
    /// Gets the output of the latest successful visit of a node, in a branch or else in the root branch.
    /// </summary>
    /// <param name="state">The <see cref="WorkflowRunState"/>.</param>
    /// <param name="node">The <see cref="WorkflowNodeId"/>.</param>
    /// <param name="branch">The <see cref="WorkflowBranchKey"/> to look in first.</param>
    /// <returns>The output, or null when the node has not succeeded in either.</returns>
    public static WorkflowData? LatestOutput(WorkflowRunState state, WorkflowNodeId node, WorkflowBranchKey branch) =>
        LatestIn(state, node, branch) ?? LatestIn(state, node, WorkflowBranchKey.Root);

    static WorkflowData? LatestIn(WorkflowRunState state, WorkflowNodeId node, WorkflowBranchKey branch) =>
        state.Executions.Values
            .Where(execution => execution.Key.Node == node && execution.Key.Branch == branch && execution.Status == NodeExecutionStatus.Succeeded)
            .MaxBy(execution => execution.Key.Visit)?.Output;

    static JsonNode? Parse(WorkflowData data)
    {
        try
        {
            return JsonNode.Parse(data.Value);
        }
        catch (JsonException)
        {
            return JsonValue.Create(data.Value);
        }
    }
}
