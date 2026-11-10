// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Sql.for_JobStepStorage.given;

namespace Cratis.Choreography.Storage.Sql.for_JobStepStorage.when_removing_steps;

/// <summary>
/// Specifies how steps are removed one at a time, all for a job, and all but the failed ones for a job.
/// </summary>
public class and_steps_have_mixed_statuses : job_steps_in_storage
{
    JobId _otherJobId;
    JobStepId _removedStep;
    int _afterRemovingOne;
    int _afterRemovingNonFailed;
    int _afterRemovingAll;
    int _otherJobAfterRemovingAll;

    async Task Establish()
    {
        _otherJobId = JobId.New();
        _removedStep = await SaveStep(_jobId, JobStepStatus.Running);
        await SaveStep(_jobId, JobStepStatus.Running);
        await SaveStep(_jobId, JobStepStatus.Failed);
        await SaveStep(_otherJobId, JobStepStatus.Running);
    }

    async Task Because()
    {
        await _storage.Remove(_jobId, _removedStep);
        _afterRemovingOne = (await _storage.CountForJob(_jobId)).AsT0;
        await _storage.RemoveAllNonFailedForJob(_jobId);
        _afterRemovingNonFailed = (await _storage.CountForJob(_jobId)).AsT0;
        await _storage.RemoveAllForJob(_jobId);
        _afterRemovingAll = (await _storage.CountForJob(_jobId)).AsT0;
        _otherJobAfterRemovingAll = (await _storage.CountForJob(_otherJobId)).AsT0;
    }

    [Fact] void should_remove_only_the_given_step() => _afterRemovingOne.ShouldEqual(2);

    [Fact] void should_keep_failed_steps_when_removing_non_failed() => _afterRemovingNonFailed.ShouldEqual(1);

    [Fact] void should_remove_failed_steps_when_removing_all() => _afterRemovingAll.ShouldEqual(0);

    [Fact] void should_not_touch_other_jobs() => _otherJobAfterRemovingAll.ShouldEqual(1);

    async Task<JobStepId> SaveStep(JobId jobId, JobStepStatus status)
    {
        var jobStepId = JobStepId.New();
        await _storage.Save(jobId, jobStepId, AJobStepState(jobId, jobStepId, status));
        return jobStepId;
    }
}
