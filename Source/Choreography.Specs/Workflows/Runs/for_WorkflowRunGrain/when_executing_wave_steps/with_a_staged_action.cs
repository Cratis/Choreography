// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Runs.for_WorkflowRunGrain.when_executing_wave_steps;

public class with_a_staged_action : given.a_run_with_wave_steps
{
    WorkflowRunSnapshot _snapshot = null!;

    void Establish() => Definition = Definition with
    {
        Graph = new WorkflowGraph(
            [Definition.Graph.Nodes.First(), Definition.Graph.Nodes.Last() with { Capability = new("staged", 1) }],
            [.. Definition.Graph.Edges])
    };

    async Task Because()
    {
        await Grain.Admit(Definition, Trigger);
        await DrainWaves();
        _snapshot = await Grain.GetSnapshot();
    }

    [Fact] void should_run_both_stages_in_order() => Stages.ShouldContainOnly(0, 10);
    [Fact] void should_record_the_intermediate_operation() => History.Select(item => item.Fact).OfType<WorkflowNodeOperationCompleted>().Count().ShouldEqual(1);
    [Fact] void should_complete_from_real_step_reports() => _snapshot.Status.ShouldEqual(WorkflowRunStatus.Succeeded);
    [Fact] void should_use_the_same_logical_effect_key_for_both_stages() => Invocations.Select(invocation => invocation.IdempotencyKey).Distinct().Count().ShouldEqual(1);
    [Fact] void should_run_in_the_durable_job_tenant() => Invocations.TrueForAll(invocation => invocation.Run.Scope == Key.Scope).ShouldBeTrue();
}
