// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.InMemory;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobsManager;

public class when_cleaning_up_orphaned_step_plans : given.the_manager
{
    JobId _missingJobId = JobId.New();
    List<JobId> _liveJobs = [];
    List<JobId> _terminalJobs = [];
    JobStepPlanStorage _plans = new();
    Guid _incarnation = Guid.NewGuid();

    protected override IJobStepPlanStorage? CreatePlanStore() => _plans;

    async Task Establish()
    {
        foreach (var status in Enum.GetValues<JobStatus>())
        {
            var id = JobId.New();
            AddJob<INullJobWithSomeRequest>(id);
            _storedJobs.Single(job => job.Id == id).Status = status;
            if (status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures or JobStatus.Failed)
            {
                _terminalJobs.Add(id);
            }
            else
            {
                _liveJobs.Add(id);
            }
        }
        var finalStop = JobId.New();
        AddJob<INullJobWithSomeRequest>(finalStop);
        var stopped = _storedJobs.Single(job => job.Id == finalStop);
        stopped.Status = JobStatus.Stopped;
        stopped.StopFinalized = true;
        _terminalJobs.Add(finalStop);
        foreach (var id in _liveJobs.Concat(_terminalJobs).Append(_missingJobId))
        {
            await _plans.Save(id, _incarnation, [new(JobStepId.New(), new JobStepType("step"), new(0), "type", "{}")]);
        }
    }

    Task Because() => _manager.CleanupDeadJobs();

    [Fact] async Task should_remove_the_plan_of_a_missing_job() => (await _plans.GetForJob(_missingJobId, _incarnation)).AsT0.ShouldBeEmpty();
    [Fact]
    async Task should_remove_the_plans_of_terminal_jobs()
    {
        foreach (var id in _terminalJobs)
        {
            (await _plans.GetForJob(id, _incarnation)).AsT0.ShouldBeEmpty();
        }
    }
    [Fact]
    async Task should_keep_the_plan_in_every_non_terminal_status()
    {
        foreach (var id in _liveJobs)
        {
            (await _plans.GetForJob(id, _incarnation)).AsT0.Count.ShouldEqual(1);
        }
    }
}
