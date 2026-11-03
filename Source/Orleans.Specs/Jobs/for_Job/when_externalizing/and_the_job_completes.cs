// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

public class and_the_job_completes : given.a_job_with_a_plan_store
{
    async Task Establish() => await _job.Start(new());

    async Task Because()
    {
        await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());
        await _job.OnStepSucceeded(_secondId, JobStepResult.Succeeded());
        await _job.OnStepSucceeded(_laterId, JobStepResult.Succeeded());
    }

    [Fact] void should_keep_the_terminal_job() => _job.ClearAttempts.ShouldEqual(0);
    [Fact] void should_durably_complete() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_retain_the_fence_after_running() => _job.WrittenStates[^1].StoredStepPlanIncarnation.ShouldEqual(_incarnation);
    [Fact] void should_remove_only_its_own_incarnation() => _removedIncarnations.ShouldEqual<Guid>([_incarnation]);
    [Fact] async Task should_remove_the_plan() => (await _planStore.GetJobsWithPlans()).AsT0.ShouldBeEmpty();
}
