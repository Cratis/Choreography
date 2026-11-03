// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.Integration.for_JobStepPlanStorage;

[Collection(JobsClusterCollection.Name)]
public class when_saving_an_empty_plan : given.a_plan_store
{
    IReadOnlyList<PlannedJobStep> _read = [];
    IReadOnlyList<JobStepPlanKey> _jobsWithPlans = [];

    async Task Establish()
    {
        _plan = [new PlannedJobStep(JobStepId.New(), new JobStepType("step"), new(0), "request", "{}")];
        (await _store.Save(_jobId, _incarnation, _plan)).RethrowError();
    }

    async Task Because()
    {
        (await _store.Save(_jobId, _incarnation, [])).RethrowError();
        _read = (await _store.GetForJob(_jobId, _incarnation)).AsT0;
        _jobsWithPlans = (await _store.GetJobsWithPlans()).AsT0;
    }

    [Fact] void should_represent_the_plan_as_missing() => _read.ShouldBeEmpty();
    [Fact] void should_not_report_the_job_as_having_a_plan() => _jobsWithPlans.ShouldBeEmpty();
}
