// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Moq;

namespace Cratis.Choreography.Jobs.for_JobsManager;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/5762 — a job left in StartingSteps with every step
/// completed is no dead job and no terminal job, so cleanup never finalized it.
/// </summary>
public class when_cleaning_up_dead_jobs_left_starting_steps : given.the_manager
{
    JobId _completedJobId;
    JobId _unfinishedJobId;
    JobId _recentJobId;
    Mock<INullJobWithSomeRequest> _completedJob;
    Mock<INullJobWithSomeRequest> _unfinishedJob;
    Mock<INullJobWithSomeRequest> _recentJob;

    void Establish()
    {
        _completedJobId = Guid.Parse("0a4d4a53-5c3c-4c34-9d1a-6b8f0d1c2e01");
        _unfinishedJobId = Guid.Parse("0a4d4a53-5c3c-4c34-9d1a-6b8f0d1c2e02");
        _recentJobId = Guid.Parse("0a4d4a53-5c3c-4c34-9d1a-6b8f0d1c2e03");
        _completedJob = StartingJob(_completedJobId, DateTimeOffset.UtcNow.AddDays(-1), failedSteps: 1);
        _unfinishedJob = StartingJob(_unfinishedJobId, DateTimeOffset.UtcNow.AddDays(-1), failedSteps: 0);
        _recentJob = StartingJob(_recentJobId, DateTimeOffset.UtcNow, failedSteps: 1);
        _completedJob.Setup(_ => _.Resume()).ReturnsAsync(Result<ResumeJobSuccess, ResumeJobError>.Success(ResumeJobSuccess.JobIsCompleted));
    }

    Task Because() => _manager.CleanupDeadJobs();

    [Fact] void should_finalize_the_job_with_every_step_completed() => _completedJob.Verify(_ => _.Resume(), Times.Once);
    [Fact] void should_leave_a_job_with_unfinished_steps() => _unfinishedJob.Verify(_ => _.Resume(), Times.Never);
    [Fact] void should_leave_a_job_that_is_not_old_enough() => _recentJob.Verify(_ => _.Resume(), Times.Never);

    Mock<INullJobWithSomeRequest> StartingJob(JobId id, DateTimeOffset created, int failedSteps)
    {
        var job = AddJob<INullJobWithSomeRequest>(id);
        var state = _storedJobs.Single(_ => _.Id == id);
        state.Status = JobStatus.StartingSteps;
        state.Created = created;
        state.Progress = new JobProgress { TotalSteps = 1, FailedSteps = failedSteps };
        return job;
    }
}
