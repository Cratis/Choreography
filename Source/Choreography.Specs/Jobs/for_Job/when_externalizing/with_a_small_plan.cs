// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.for_Job.when_externalizing;

public class with_a_small_plan : given.a_job_with_a_plan_store
{
    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 1 };

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_keep_the_plan_inline() => _job.WrittenStates.First(state => state.Status is JobStatus.PreparingSteps).StepPlan!.Count.ShouldEqual(3);
    [Fact] async Task should_not_use_the_plan_store() => (await _planStore.GetJobsWithPlans()).AsT0.ShouldBeEmpty();
}
