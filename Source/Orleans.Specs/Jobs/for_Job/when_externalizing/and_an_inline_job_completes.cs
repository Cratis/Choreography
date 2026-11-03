// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

public class and_an_inline_job_completes : given.a_job_with_a_plan_store
{
    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 1 };

    async Task Establish() => await _job.Start(new());

    async Task Because()
    {
        await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());
        await _job.OnStepSucceeded(_secondId, JobStepResult.Succeeded());
        await _job.OnStepSucceeded(_laterId, JobStepResult.Succeeded());
    }

    [Fact] void should_complete() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.CompletedSuccessfully);
    [Fact] void should_not_request_plan_cleanup() => _planRemovals.ShouldEqual(0);
}
