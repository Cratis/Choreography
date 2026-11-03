// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

public class and_plan_removal_throws_at_completion : given.a_job_with_a_plan_store
{
    Result<JobError> _completion;

    async Task Establish()
    {
        await _job.Start(new());
        _planRemovalThrows = true;
    }

    async Task Because()
    {
        await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());
        await _job.OnStepSucceeded(_secondId, JobStepResult.Succeeded());
        _completion = await _job.OnStepSucceeded(_laterId, JobStepResult.Succeeded());
    }

    [Fact] void should_acknowledge_completion() => _completion.IsSuccess.ShouldBeTrue();
    [Fact] void should_durably_complete() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] async Task should_leave_the_plan_for_cleanup() => (await PlanIsStored()).ShouldBeTrue();
    [Fact] void should_attempt_plan_removal() => (_planRemovals > 0).ShouldBeTrue();
}
