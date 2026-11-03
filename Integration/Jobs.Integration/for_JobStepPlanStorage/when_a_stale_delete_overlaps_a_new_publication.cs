// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Orleans.Jobs.Integration.for_JobsManager.given;
using Cratis.Orleans.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Jobs.Integration.for_JobStepPlanStorage;

[Collection(JobsClusterCollection.Name)]
public class when_a_stale_delete_overlaps_a_new_publication : given.a_plan_store
{
    Guid _replacement = Guid.NewGuid();
    PlannedJobStep[] _replacementPlan = [new(JobStepId.New(), new JobStepType("replacement"), new(0), "request", "{}")];
    JobsManager _manager;
    JobsStorage _storage;
    TaskCompletionSource _deleting = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource _continue = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        LargePlan();
        _storage = JobsClusterFixture.Shared.Services.GetRequiredService<IJobsStorage>().GetFor(JobsClusterFixture.Scope, string.Empty);
        _manager = new JobsManager(Substitute.For<IJobsStorage>(), Substitute.For<IJobTypes>(), Options.Create(new JobsOptions { TerminalJobRetention = Timeout.InfiniteTimeSpan }), Substitute.For<ILogger<JobsManager>>());

        // Bind the real manager to Mongo storage with only its plan collection decorated, without scheduling timers.
        typeof(JobsManager).GetField("_jobsStorageForScope", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_manager, _storage with { StepPlans = _faultingStore });
        typeof(JobsManager).GetField("_jobStorage", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_manager, _storage.Jobs);
        typeof(JobsManager).GetField("_jobStepStorage", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_manager, _storage.JobSteps);
        (await _store.Save(_jobId, _incarnation, _plan)).RethrowError();
        _beforeDelete = async () =>
        {
            _deleting.TrySetResult();
            await _continue.Task.WaitAsync(TimeSpan.FromSeconds(10));
        };
    }

    async Task Because()
    {
        var deletion = _manager.CleanupDeadJobs();
        try
        {
            await _deleting.Task.WaitAsync(TimeSpan.FromSeconds(10));
            (await _storage.Jobs.Save(_jobId, new JobState
            {
                Id = _jobId,
                Type = typeof(JobWithSingleStep),
                Status = JobStatus.PreparingJob,
                StoredStepPlanIncarnation = _replacement,
                Created = DateTimeOffset.UtcNow
            })).RethrowError();
            (await _store.Save(_jobId, _replacement, _replacementPlan)).RethrowError();
        }
        finally
        {
            _continue.TrySetResult();
        }
        await deletion;
    }

    [Fact] async Task should_leave_the_new_publication_complete() => (await _store.GetForJob(_jobId, _replacement)).AsT0.ShouldEqual<PlannedJobStep>(_replacementPlan);
    [Fact] async Task should_delete_only_the_old_publication() => (await _store.GetForJob(_jobId, _incarnation)).AsT0.ShouldBeEmpty();
    [Fact] async Task should_enumerate_the_remaining_document_identity() => (await _store.GetJobsWithPlans()).AsT0.ShouldEqual<JobStepPlanKey>([new(_jobId, _replacement)]);
    [Fact] async Task should_not_load_an_unrelated_incarnation() => (await _store.GetForJob(_jobId, Guid.NewGuid())).AsT0.ShouldBeEmpty();
}
