// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.Stages;
using Moq;

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

public class and_only_another_incarnation_is_stored : given.a_job_with_a_plan_store
{
    async Task Establish()
    {
        _job.RejectWrite = state => state.Status is JobStatus.Failed;
        _first.Setup(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()))
            .ThrowsAsync(new InvalidOperationException("The silo stopped"));
        await _job.Start(new());
        var snapshot = _job.WrittenStates.Last(state => state.Status is JobStatus.PreparingSteps);
        snapshot.StoredStepPlanIncarnation = Guid.NewGuid();
        await ActivateFrom(snapshot);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_report_its_own_plan_missing() => _result.AsT1.AsT0.ShouldEqual(CannotResumeJobError.MissingStepPlan);
    [Fact] async Task should_leave_the_other_plan_untouched() => (await PlanIsStored()).ShouldBeTrue();
    [Fact] void should_not_complete() => _job.AllStepsCompletedCount.ShouldEqual(0);
}
