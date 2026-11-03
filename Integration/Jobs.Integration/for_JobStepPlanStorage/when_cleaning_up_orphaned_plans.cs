// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.for_JobsManager.given;
using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs.Integration.for_JobStepPlanStorage;

[Collection(JobsClusterCollection.Name)]
public class when_cleaning_up_orphaned_plans : given.a_plan_store
{
    JobId _running = JobId.New();
    JobId _terminal = JobId.New();

    async Task Establish()
    {
        var storage = JobsClusterFixture.Shared.Services.GetRequiredService<IJobsStorage>()
            .GetFor(JobsClusterFixture.Scope, string.Empty).Jobs;
        foreach (var (id, status) in new[] { (_running, JobStatus.Running), (_terminal, JobStatus.CompletedSuccessfully) })
        {
            (await storage.Save(id, new JobState { Id = id, Type = typeof(JobWithSingleStep), Status = status })).RethrowError();
        }
        foreach (var id in new[] { _jobId, _running, _terminal })
        {
            (await _store.Save(id, _incarnation, [new PlannedJobStep(JobStepId.New(), new JobStepType("step"), new(0), "request", "{}")])).RethrowError();
        }
    }

    Task Because() => JobsClusterFixture.Shared.GrainFactory.GetJobsManager(JobsClusterFixture.Scope, string.Empty).CleanupDeadJobs();

    [Fact] async Task should_remove_a_missing_jobs_plan() => (await _store.GetForJob(_jobId, _incarnation)).AsT0.ShouldBeEmpty();
    [Fact] async Task should_remove_a_terminal_jobs_plan() => (await _store.GetForJob(_terminal, _incarnation)).AsT0.ShouldBeEmpty();
    [Fact] async Task should_retain_a_running_jobs_plan() => (await _store.GetForJob(_running, _incarnation)).AsT0.Count.ShouldEqual(1);
}
