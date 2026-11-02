// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.InMemory;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_cleaning_up_orphaned_step_plans : given.the_manager
{
    static readonly JobId _missingJobId = Guid.Parse("0e6a1e0b-3c0c-4d0b-8f6f-7d9e2b5a1c01");
    static readonly JobId _runningJobId = Guid.Parse("0e6a1e0b-3c0c-4d0b-8f6f-7d9e2b5a1c02");
    static readonly JobId _failedJobId = Guid.Parse("0e6a1e0b-3c0c-4d0b-8f6f-7d9e2b5a1c03");
    static readonly JobId _preparingJobId = Guid.Parse("0e6a1e0b-3c0c-4d0b-8f6f-7d9e2b5a1c04");
    JobStepPlanStorage _plans = new();

    protected override IJobStepPlanStorage? CreatePlanStore() => _plans;

    async Task Establish()
    {
        AddJob<INullJobWithSomeRequest>(_runningJobId);
        AddJob<INullJobWithSomeRequest>(_failedJobId);
        AddJob<INullJobWithSomeRequest>(_preparingJobId);
        _storedJobs.Single(job => job.Id == _failedJobId).Status = JobStatus.Failed;
        _storedJobs.Single(job => job.Id == _preparingJobId).Status = JobStatus.PreparingSteps;
        foreach (var id in new[] { _missingJobId, _runningJobId, _failedJobId, _preparingJobId })
        {
            await _plans.Save(id, [new(JobStepId.New(), new JobStepType("step"), new(0), "type", "{}")]);
        }
    }

    Task Because() => _manager.CleanupDeadJobs();

    [Fact] async Task should_remove_the_plan_of_a_missing_job() => (await _plans.GetForJob(_missingJobId)).AsT0.ShouldBeEmpty();
    [Fact] async Task should_remove_the_plan_of_a_running_job() => (await _plans.GetForJob(_runningJobId)).AsT0.ShouldBeEmpty();
    [Fact] async Task should_remove_the_plan_of_a_terminal_job() => (await _plans.GetForJob(_failedJobId)).AsT0.ShouldBeEmpty();
    [Fact] async Task should_keep_the_plan_of_a_preparing_job() => (await _plans.GetForJob(_preparingJobId)).AsT0.Count.ShouldEqual(1);
}
