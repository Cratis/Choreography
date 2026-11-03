// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Integration.for_JobsManager.given;
using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Orleans.Jobs.Integration.for_JobStepPlanStorage;

[Collection(JobsClusterCollection.Name)]
public class when_cleanup_overlaps_a_plan_save : given.a_plan_store
{
    TaskCompletionSource _published = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource _continue = new(TaskCreationOptions.RunContinuationsAsynchronously);
    long _retainedDuringSave;

    async Task Establish()
    {
        LargePlan();
        var storage = JobsClusterFixture.Shared.Services.GetRequiredService<IJobsStorage>()
            .GetFor(JobsClusterFixture.Scope, string.Empty).Jobs;
        (await storage.Save(_jobId, new JobState
        {
            Id = _jobId,
            Type = typeof(JobWithSingleStep),
            Status = JobStatus.PreparingJob,
            Created = DateTimeOffset.UtcNow
        })).RethrowError();
        _afterInsert = async attempt =>
        {
            if (attempt == 1)
            {
                _published.SetResult();
                await _continue.Task;
            }
        };
    }

    async Task Because()
    {
        var save = _faultingStore.Save(_jobId, _plan);
        try
        {
            await _published.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await JobsClusterFixture.Shared.GrainFactory.GetJobsManager(JobsClusterFixture.Scope, string.Empty).CleanupDeadJobs();
            _retainedDuringSave = await _collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        }
        finally
        {
            _continue.TrySetResult();
        }
        (await save).RethrowError();
    }

    [Fact] void should_keep_the_published_batch_while_the_job_is_preparing() => (_retainedDuringSave > 0).ShouldBeTrue();
    [Fact] Task should_finish_with_a_complete_recoverable_plan() => AssertStoredPlan();
}
