// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Choreography.Storage.InMemory.for_JobStepStorage.when_removing_steps;

public class and_steps_are_in_both_collections : Specification
{
    JobStepStorage _storage;
    JobId _jobId;
    JobId _otherJobId;
    JobStepId _removedStep;
    int _afterRemovingOne;
    int _afterRemovingNonFailed;
    int _afterRemovingAll;
    int _otherJobAfterRemovingAll;

    async Task Establish()
    {
        _storage = new();
        _jobId = JobId.New();
        _otherJobId = JobId.New();
        _removedStep = await Save(_jobId, JobStepStatus.Running);
        await Save(_jobId, JobStepStatus.Running);
        await Save(_jobId, JobStepStatus.CompletedWithFailure);
        await Save(_otherJobId, JobStepStatus.Running);
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

    async Task<JobStepId> Save(JobId jobId, JobStepStatus status)
    {
        var stepId = JobStepId.New();
        await _storage.Save(jobId, stepId, new JobStepState { Id = new(jobId, stepId), Type = typeof(IJobStep), Status = status });
        return stepId;
    }
}
