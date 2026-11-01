// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Runs.for_WorkflowRunGrain.when_a_deadline_is_pending;

public class and_the_sweep_tends_the_run : given.a_run_that_wakes_itself
{
    WorkflowRunSnapshot _after = null!;

    async Task Because()
    {
        await Grain.Admit(Definition, Trigger);
        await Grain.Tend(DateTimeOffset.UtcNow.AddMinutes(1));
        _after = await Grain.GetSnapshot();
    }

    [Fact] void should_expire_the_wait() => _after.Status.ShouldEqual(WorkflowRunStatus.Succeeded);
}
