// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.for_Job.when_externalizing;

public class and_a_silo_lacks_the_capability_and_the_document_is_too_large : given.a_job_with_a_plan_store
{
    void Establish()
    {
        _supportedByEverySilo = false;
        _job.RejectWrite = state => state.Status is JobStatus.PreparingSteps && state.StepPlan is not null;
    }

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_fail_the_start() => _start.IsSuccess.ShouldBeFalse();
    [Fact] void should_durably_fail_the_job() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_record_a_reason() => _job.WrittenStates[^1].StatusChanges[^1].ExceptionMessages.ShouldNotBeEmpty();
    [Fact] void should_keep_the_plan_out_of_the_failed_write() => _job.WrittenStates[^1].StepPlan.ShouldBeNull();
    [Fact] async Task should_not_save_a_plan_separately() => (await _planStore.GetJobsWithPlans()).AsT0.ShouldBeEmpty();
}
