// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

public class with_an_oversized_plan_and_every_silo_capable_after_running : given.a_job_with_a_plan_store
{
    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_be_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] async Task should_remove_the_plan_after_running() => (await _planStore.GetJobsWithPlans()).AsT0.ShouldBeEmpty();
    [Fact] void should_not_flag_a_stored_plan() => _job.CurrentState.HasStoredStepPlan.ShouldBeFalse();
}
