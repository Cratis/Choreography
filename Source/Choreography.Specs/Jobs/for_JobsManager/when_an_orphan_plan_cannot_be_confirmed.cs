// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage.InMemory;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Monads;

namespace Cratis.Choreography.Jobs.for_JobsManager;

public class when_an_orphan_plan_cannot_be_confirmed : given.the_manager
{
    JobStepPlanStorage _plans = new();
    Guid _incarnation = Guid.NewGuid();
    JobId _changed = JobId.New();
    JobId _unreadable = JobId.New();
    JobId _throwing = JobId.New();

    protected override IJobStepPlanStorage? CreatePlanStore() => _plans;

    async Task Establish()
    {
        foreach (var id in new[] { _changed, _unreadable, _throwing })
        {
            await _plans.Save(id, _incarnation, [new(JobStepId.New(), new JobStepType("step"), new(0), "type", "{}")]);
        }
        _jobStorage.GetJob(_changed).Returns(
            Task.FromResult<Catch<JobState, Storage.Jobs.JobError>>(Storage.Jobs.JobError.NotFound),
            Task.FromResult<Catch<JobState, Storage.Jobs.JobError>>(new JobState { Id = _changed, Status = JobStatus.PreparingJob }));
        _jobStorage.GetJob(_unreadable).Returns(Task.FromResult<Catch<JobState, Storage.Jobs.JobError>>(new InvalidOperationException("Unreadable")));
        _jobStorage.GetJob(_throwing).Returns(Task.FromException<Catch<JobState, Storage.Jobs.JobError>>(new InvalidOperationException("Read failed")));
    }

    Task Because() => _manager.CleanupDeadJobs();

    [Fact] async Task should_recheck_the_job_before_deletion() => (await _plans.GetForJob(_changed, _incarnation)).AsT0.Count.ShouldEqual(1);
    [Fact] async Task should_keep_a_plan_when_the_read_returns_an_error() => (await _plans.GetForJob(_unreadable, _incarnation)).AsT0.Count.ShouldEqual(1);
    [Fact] async Task should_keep_a_plan_when_the_read_throws() => (await _plans.GetForJob(_throwing, _incarnation)).AsT0.Count.ShouldEqual(1);
}
