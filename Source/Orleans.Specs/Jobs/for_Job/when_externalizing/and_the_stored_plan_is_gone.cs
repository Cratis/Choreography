// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

/// <summary>
/// A job whose durable state refers to a plan that is no longer in the store must report it missing, not finish with no steps.
/// </summary>
public class and_the_stored_plan_is_gone : given.a_job_with_a_plan_store
{
    async Task Establish()
    {
        _job.RejectWrite = state => state.Status is JobStatus.Failed;
        _first.Setup(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()))
            .ThrowsAsync(new InvalidOperationException("The silo stopped"));
        await _job.Start(new());
        await ActivateFrom(JobStatus.PreparingSteps);
        await _planStore.RemoveForJob(_jobId, _incarnation);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_fail_with_a_missing_step_plan() => _result.AsT1.AsT0.ShouldEqual(CannotResumeJobError.MissingStepPlan);
    [Fact] void should_not_complete_the_job() => _job.AllStepsCompletedCount.ShouldEqual(0);
    [Fact] void should_not_be_completed() => _job.CurrentState.Status.ShouldEqual(JobStatus.PreparingSteps);
}
