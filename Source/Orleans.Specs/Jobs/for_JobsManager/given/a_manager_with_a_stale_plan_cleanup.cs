// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.InMemory;
using Cratis.Orleans.Storage.Jobs;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Orleans.Jobs.for_JobsManager.given;

public class a_manager_with_a_stale_plan_cleanup : the_manager
{
    protected JobId _jobId = JobId.New();
    protected Guid _oldIncarnation = Guid.NewGuid();
    protected Guid _newIncarnation = Guid.NewGuid();
    protected JobStepPlanStorage _plans = new();
    protected PlannedJobStep _newStep = new(JobStepId.New(), new JobStepType("step"), new(0), "type", "{}");
    TaskCompletionSource _deleting = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource _continue = new(TaskCreationOptions.RunContinuationsAsynchronously);

    protected virtual bool HasTerminalJob => false;

    protected override IJobStepPlanStorage? CreatePlanStore() => new PausedRemoval(this);

    async Task Establish()
    {
        if (HasTerminalJob)
        {
            _storedJobs.Add(new JobState { Id = _jobId, Status = JobStatus.CompletedSuccessfully, StoredStepPlanIncarnation = _oldIncarnation });
        }
        (await _plans.Save(_jobId, _oldIncarnation, [new(JobStepId.New(), new JobStepType("old"), new(0), "type", "{}")])).RethrowError();
    }

    protected async Task CleanupAcrossReplacement()
    {
        var cleanup = _manager.CleanupDeadJobs();
        try
        {
            // The manager has completed its last eligibility read, but the storage delete has not begun.
            await _deleting.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
            _storedJobs.RemoveAll(job => job.Id == _jobId);
            _storedJobs.Add(new JobState { Id = _jobId, Status = JobStatus.PreparingJob, StoredStepPlanIncarnation = _newIncarnation });
            (await _plans.Save(_jobId, _newIncarnation, [_newStep])).RethrowError();
        }
        finally
        {
            _continue.TrySetResult();
        }
        await cleanup;
    }

    protected async Task AssertReplacement() => (await _plans.GetForJob(_jobId, _newIncarnation)).AsT0.ShouldEqual<PlannedJobStep>([_newStep]);

    protected async Task AssertOldRemoved() => (await _plans.GetForJob(_jobId, _oldIncarnation)).AsT0.ShouldBeEmpty();

    sealed class PausedRemoval(a_manager_with_a_stale_plan_cleanup owner) : IJobStepPlanStorage
    {
        public Task<Catch> Save(JobId jobId, Guid incarnation, IReadOnlyList<PlannedJobStep> plan) => owner._plans.Save(jobId, incarnation, plan);

        public Task<Catch<IReadOnlyList<PlannedJobStep>>> GetForJob(JobId jobId, Guid incarnation) => owner._plans.GetForJob(jobId, incarnation);

        public Task<Catch<IReadOnlyList<JobStepPlanKey>>> GetJobsWithPlans() => owner._plans.GetJobsWithPlans();

        public async Task<Catch> RemoveForJob(JobId jobId, Guid incarnation)
        {
            owner._deleting.TrySetResult();
            await owner._continue.Task.WaitAsync(TimeSpan.FromSeconds(10));
            return await owner._plans.RemoveForJob(jobId, incarnation);
        }
    }
}
