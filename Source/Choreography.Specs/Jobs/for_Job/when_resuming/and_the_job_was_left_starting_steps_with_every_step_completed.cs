// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Choreography.Jobs.for_Job.when_resuming;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/5762 — a job left in StartingSteps with every step
/// already accounted for was recovered as a startup, so it was neither resumed nor finalized.
/// </summary>
public class and_the_job_was_left_starting_steps_with_every_step_completed : given.the_job
{
    JobStepId _jobStepId;
    Mock<given.ISomeJobStep> _jobStep;
    Result<ResumeJobSuccess, ResumeJobError> _result;

    void Establish()
    {
        _job.ShouldBeResumable = true;
        _jobStepId = Guid.Parse("3f3f3f3f-0000-0000-0000-000000000005");
        _jobStep = AddJobStep(_jobStepId);
        _jobStep.Setup(_ => _.Start(It.IsAny<GrainId>())).ReturnsAsync(Result<StartJobStepError>.Success());
    }

    async Task Because()
    {
        await _job.Start(new());

        // The only step failed and reported, and the status that follows from it was never written.
        _job.CurrentState.Status = JobStatus.StartingSteps;
        _job.CurrentState.Progress.TotalSteps = 1;
        _job.CurrentState.Progress.SuccessfulSteps = 0;
        _job.CurrentState.Progress.FailedSteps = 1;

        _result = await _job.Resume();
    }

    [Fact] void should_report_the_job_as_completed() => ((ResumeJobSuccess)_result).ShouldEqual(ResumeJobSuccess.JobIsCompleted);
    [Fact] void should_finalize_the_job_with_failures() => _job.CurrentState.Status.ShouldEqual(JobStatus.CompletedWithFailures);
}
