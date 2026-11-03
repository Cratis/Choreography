// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.for_Job.when_externalizing;

public class with_an_oversized_plan_and_every_silo_capable : given.a_job_with_a_plan_store
{
    List<JobStepId> _planWhenPreparingWasDurable = [];

    void Establish() => _job.AfterWrite = async () =>
    {
        if (_job.CurrentState.Status is JobStatus.PreparingSteps)
        {
            _planWhenPreparingWasDurable = (await _planStore.GetForJob(_jobId, _incarnation)).AsT0.Select(step => step.Id).ToList();
        }
    };

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_start() => _start.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_plan_out_of_the_preparing_document() => _job.WrittenStates.First(state => state.Status is JobStatus.PreparingSteps).StepPlan.ShouldBeNull();
    [Fact] void should_flag_the_stored_plan_in_the_preparing_document() => _job.WrittenStates.First(state => state.Status is JobStatus.PreparingSteps).HasStoredStepPlan.ShouldBeTrue();
    [Fact] void should_have_saved_the_plan_in_order_before_preparing_was_durable() => _planWhenPreparingWasDurable.ShouldEqual([_firstId, _secondId, _laterId]);
}
