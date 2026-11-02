// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.for_Job.given;
using Cratis.Orleans.Jobs.Stages;
using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

/// <summary>
/// A resume from StartingSteps, with a step that already completed and deleted its state, has its Running write rejected without persisting.
/// The activation retires and persists nothing more, and the next activation recovers from the durable state and counts the completed step.
/// </summary>
public class and_the_running_write_is_not_committed : given.a_job_with_a_plan_store
{
    SomeJobState _durable;
    SomeJob _retired;
    TestKitSilo _retiredSilo;
    int _writesAfterFailure;

    async Task Establish()
    {
        _planRemovalFails = true;
        await _job.Start(new());
        _storedJobStepStates.Remove(StoredJobStep(_firstId));
        _durable = _job.WrittenStates.Last(state => state.Status is JobStatus.StartingSteps);
        await ActivateFrom(_durable);
        _retired = _job;
        _retiredSilo = _silo;
        _retired.RejectWrite = state => state.Status is JobStatus.Running;
        (await _retired.Resume()).IsSuccess.ShouldBeFalse();
        _writesAfterFailure = _retired.WriteAttempts;

        // A report that reaches the retiring activation.
        await _retired.OnStepSucceeded(_secondId, JobStepResult.Succeeded());
        _retired.WriteAttempts.ShouldEqual(_writesAfterFailure);
        await ActivateFrom(_durable);
    }

    async Task Because() => _result = await _job.Resume();

    [Fact] void should_retire_the_activation() => _retiredSilo.GrainRuntime.Mock.Verify(runtime => runtime.DeactivateOnIdle(It.IsAny<IGrainContext>()), Times.Once);
    [Fact] void should_persist_nothing_in_the_retired_activation() => _retired.WrittenStates.ShouldBeEmpty();
    [Fact] void should_not_attempt_a_write_after_the_failure() => _retired.WriteAttempts.ShouldEqual(_writesAfterFailure);
    [Fact] async Task should_keep_the_plan_for_the_next_activation() => (await PlanIsStored()).ShouldBeTrue();
    [Fact] void should_recover_in_the_next_activation() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_count_the_completed_step() => _job.CurrentState.Progress.SuccessfulSteps.ShouldEqual(1);
    [Fact] void should_not_recreate_the_completed_step() => _first.Verify(step => step.Prepare(It.IsAny<object>(), It.IsAny<JobStepStage>()), Times.Never);
    [Fact] void should_run_from_durable_running() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
}
