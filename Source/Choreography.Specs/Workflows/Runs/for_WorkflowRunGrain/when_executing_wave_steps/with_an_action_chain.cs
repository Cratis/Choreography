// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.for_WorkflowRunGrain.when_executing_wave_steps;

public class with_an_action_chain : given.a_run_with_wave_steps
{
    WorkflowRunSnapshot _snapshot = null!;

    void Establish() => Definition = Definition with
    {
        Graph = new WorkflowGraph(
            [.. Definition.Graph.Nodes, Definition.Graph.Nodes.Last() with { Id = "second" }],
            [.. Definition.Graph.Edges, new WorkflowEdge("action", "next", "second")])
    };

    async Task Because()
    {
        await Grain.Admit(Definition, Trigger);
        await DrainWaves();
        _snapshot = await Grain.GetSnapshot();
    }

    [Fact] void should_start_two_job_waves() => History.Select(item => item.Fact).OfType<WorkflowNodeScheduled>().Count().ShouldEqual(2);
    [Fact] void should_reach_both_performers() => Invocations.Count.ShouldEqual(2);
    [Fact] void should_pass_the_first_output_to_the_second() => Invocations[^1].Input.Value.ShouldContain("\"performed\":true");
    [Fact] void should_finish_from_real_step_reports() => _snapshot.Status.ShouldEqual(WorkflowRunStatus.Succeeded);
}
