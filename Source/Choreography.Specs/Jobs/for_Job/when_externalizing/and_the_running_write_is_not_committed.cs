// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs.Stages;
using Moq;

namespace Cratis.Choreography.Jobs.for_Job.when_externalizing;

/// <summary>
/// A rejected Running write restores the stored plan reference; the next activation can recover from StartingSteps.
/// </summary>
public class and_the_running_write_is_not_committed : given.a_job_with_a_plan_store
{
    bool _restoredStoredPlan;

    async Task Establish()
    {
        await _job.Start(new());
        _storedJobStepStates.Remove(StoredJobStep(_firstId));
        var durable = _job.WrittenStates.Last(state => state.Status is JobStatus.StartingSteps);
        await ActivateFrom(durable);
        _job.RejectWrite = state => state.Status is JobStatus.Running;
        (await _job.Resume()).IsSuccess.ShouldBeFalse();
        _restoredStoredPlan = _job.CurrentState.HasStoredStepPlan;
        await ActivateFrom(durable);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_restore_the_plan_reference() => _restoredStoredPlan.ShouldBeTrue();
    [Fact] async Task should_keep_the_plan_for_the_next_activation() => (await PlanIsStored()).ShouldBeTrue();
    [Fact] void should_recover_in_the_next_activation() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_the_completed_step() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_not_recreate_the_completed_step() => _first.Verify(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()), Times.Never);
    [Fact] void should_run_from_durable_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
