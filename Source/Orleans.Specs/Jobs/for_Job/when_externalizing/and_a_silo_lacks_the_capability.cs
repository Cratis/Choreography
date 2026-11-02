// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

public class and_a_silo_lacks_the_capability : given.a_job_with_a_plan_store
{
    void Establish() => _supportedByEverySilo = false;

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_start() => _start.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_plan_inline() => _job.WrittenStates.First(state => state.Status is JobStatus.PreparingSteps).StepPlan!.Count.ShouldEqual(3);
    [Fact] void should_not_flag_a_stored_plan() => _job.WrittenStates.First(state => state.Status is JobStatus.PreparingSteps).HasStoredStepPlan.ShouldBeFalse();
    [Fact] async Task should_not_save_a_plan_separately() => (await _planStore.GetJobsWithPlans()).AsT0.ShouldBeEmpty();
}
