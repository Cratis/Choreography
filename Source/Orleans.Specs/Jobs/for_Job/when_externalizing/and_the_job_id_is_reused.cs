// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

public class and_the_job_id_is_reused : given.a_job_with_a_plan_store
{
    Guid _previous;

    async Task Establish()
    {
        await _job.Start(new());
        _previous = _incarnation;
        var steps = _job.StepsToPrepare.ToArray();
        await ActivateFrom(new given.SomeJobState());
        _job.StepsToPrepare.AddRange(steps);
    }

    async Task Because() => await _job.Start(new());

    [Fact] void should_publish_with_a_fresh_fence() => (_incarnation != _previous).ShouldBeTrue();
    [Fact] void should_persist_the_new_fence_before_saving() => _stateWasDurableBeforePlanSave.ShouldBeTrue();
    [Fact] async Task should_keep_the_new_plan_loadable() => (await PlanIsStored()).ShouldBeTrue();
}
