// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.for_WorkflowRunGrain.given;

/// <summary>
/// A run whose workflow waits on a timer that is due the moment it opens.
/// </summary>
public class a_run_that_wakes_itself : a_run
{
    protected override bool WakesItself => true;

    void Establish() => Definition = Definition with
    {
        Graph = new WorkflowGraph(
            [
                new WorkflowNode("trigger", WorkflowNodeKind.Trigger, new("manual", 1), WorkflowNodeConfiguration.Empty, ["next"], WorkflowNodePolicy.Default),
                new WorkflowNode("timer", WorkflowNodeKind.Wait, new("workflows.wait.timer", 1), "{\"kind\":\"timer\",\"after\":\"00:00:00\"}", ["elapsed"], WorkflowNodePolicy.Default)
            ],
            [new WorkflowEdge("trigger", "next", "timer")])
    };
}
