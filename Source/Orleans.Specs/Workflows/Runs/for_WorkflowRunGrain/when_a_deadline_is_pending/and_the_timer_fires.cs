// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.for_WorkflowRunGrain.when_a_deadline_is_pending;

public class and_the_timer_fires : given.a_run_that_wakes_itself
{
    WorkflowRunSnapshot _before = null!;
    WorkflowRunSnapshot _after = null!;

    async Task Because()
    {
        await Grain.Admit(Definition, Trigger);
        _before = await Grain.GetSnapshot();
        await Silo.TimerRegistry.FireAllAsync();
        _after = await Grain.GetSnapshot();
    }

    [Fact] void should_be_waiting_until_the_deadline() => _before.Status.ShouldEqual(WorkflowRunStatus.Waiting);
    [Fact] void should_act_on_the_deadline_by_itself() => _after.Status.ShouldEqual(WorkflowRunStatus.Succeeded);
}
