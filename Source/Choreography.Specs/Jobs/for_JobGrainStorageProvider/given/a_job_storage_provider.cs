// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage;
using Cratis.Choreography.Storage.Jobs;

namespace Cratis.Choreography.Jobs.for_JobGrainStorageProvider.given;

public class a_job_storage_provider : Specification
{
    protected JobGrainStorageProvider _provider;
    protected IJobStepPlanStorage _plans;
    protected JobId _jobId = JobId.New();
    protected Guid? _incarnation = Guid.NewGuid();
    protected GrainId _grainId;
    protected List<string> _removals = [];

    void Establish()
    {
        var key = new JobKey("scope", "namespace");
        _grainId = GrainId.Create(GrainType.Create("job"), GrainIdKeyExtensions.CreateGuidKey(_jobId, key));
        var jobs = Substitute.For<IJobStorage>();
        jobs.Remove(_jobId).Returns(_ =>
        {
            _removals.Add("job");
            return Task.FromResult(Monads.Catch.Success());
        });
        var steps = Substitute.For<IJobStepStorage>();
        steps.RemoveAllForJob(_jobId).Returns(_ =>
        {
            _removals.Add("steps");
            return Task.FromResult(Monads.Catch.Success());
        });
        _plans = Substitute.For<IJobStepPlanStorage>();
        var storage = Substitute.For<IJobsStorage>();
        storage.GetFor(key.Scope, key.Namespace).Returns(new JobsStorage(jobs, steps) { StepPlans = _plans });
        _provider = new(storage);
    }

    protected Task Clear()
    {
        var state = Substitute.For<IGrainState<JobState>>();
        state.State.Returns(new JobState { StoredStepPlanIncarnation = _incarnation });
        return _provider.ClearStateAsync("state", _grainId, state);
    }
}
