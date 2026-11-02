// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

public class and_the_job_recovers_from_a_stored_plan : given.a_job_with_a_plan_store
{
    async Task Establish()
    {
        // The process dies while preparing: the Failed write never happens, so the plan must remain.
        _job.RejectWrite = state => state.Status is JobStatus.Failed;
        _first.Setup(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()))
            .ThrowsAsync(new InvalidOperationException("The silo stopped"));
        await _job.Start(new());
        await ActivateFrom(JobStatus.PreparingSteps);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_resume() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_replan() => _job.PrepareStepsCount.ShouldEqual(0);
    [Fact] void should_dispatch_only_the_first_stage() => _dispatched.ShouldEqual(new[] { _firstId, _secondId });
    [Fact] async Task should_remove_the_plan_once_running() => (await _planStore.GetJobsWithPlans()).AsT0.ShouldBeEmpty();
}
