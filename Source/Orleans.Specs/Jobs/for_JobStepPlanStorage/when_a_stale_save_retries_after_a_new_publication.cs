// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Storage.InMemory;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Jobs.for_JobStepPlanStorage;

public class when_a_stale_save_retries_after_a_new_publication : Specification
{
    JobStepPlanStorage _store = new();
    JobId _jobId = JobId.New();
    Guid _old = Guid.NewGuid();
    Guid _current = Guid.NewGuid();
    PlannedJobStep _oldStep = new(JobStepId.New(), new JobStepType("old"), new(0), "type", "{}");
    PlannedJobStep _currentStep = new(JobStepId.New(), new JobStepType("current"), new(0), "type", "{}");

    async Task Establish()
    {
        (await _store.Save(_jobId, _old, [_oldStep])).RethrowError();
        (await _store.Save(_jobId, _current, [_currentStep])).RethrowError();
    }

    async Task Because() => (await _store.Save(_jobId, _old, [_oldStep])).RethrowError();

    [Fact] async Task should_not_replace_the_new_plan() => (await _store.GetForJob(_jobId, _current)).AsT0.ShouldEqual<PlannedJobStep>([_currentStep]);
    [Fact] async Task should_replace_only_its_own_attempt() => (await _store.GetForJob(_jobId, _old)).AsT0.ShouldEqual<PlannedJobStep>([_oldStep]);
    [Fact] async Task should_enumerate_both_orphan_document_identities() => (await _store.GetJobsWithPlans()).AsT0.Count.ShouldEqual(2);
}
