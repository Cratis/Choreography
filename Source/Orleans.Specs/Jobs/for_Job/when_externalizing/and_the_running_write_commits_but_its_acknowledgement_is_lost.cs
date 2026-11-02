// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs.for_Job.given;
using Moq;
using Orleans.TestKit;

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

/// <summary>
/// The Running write commits without a plan reference, then throws. Orphan cleanup then deletes the plan. The activation retires and a step
/// report arriving later cannot persist anything, in particular not a reference to the deleted plan.
/// </summary>
public class and_the_running_write_commits_but_its_acknowledgement_is_lost : given.a_job_with_a_plan_store
{
    int _writesBeforeReport;
    int _attemptsBeforeReport;
    TestKitSilo _retiredSilo;
    SomeJob _retired;

    async Task Establish()
    {
        _job.PersistBeforeFailingWrite = true;
        _job.RejectWrite = state => state.Status is JobStatus.Running && !state.HasStoredStepPlan;
        _start = await _job.Start(new());
        await _planStore.RemoveForJob(_jobId);
        _writesBeforeReport = _job.WrittenStates.Count;
        _attemptsBeforeReport = _job.WriteAttempts;
        _job.RejectWrite = null;
        _retired = _job;
        _retiredSilo = _silo;
    }

    async Task Because() => await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());

    [Fact] void should_fail_the_start() => _start.IsSuccess.ShouldBeFalse();
    [Fact] void should_retire_the_activation() => _retiredSilo.GrainRuntime.Mock.Verify(runtime => runtime.DeactivateOnIdle(It.IsAny<IGrainContext>()), Times.Once);
    [Fact] void should_not_attempt_a_write_after_the_failure() => _retired.WriteAttempts.ShouldEqual(_attemptsBeforeReport);
    [Fact] void should_persist_nothing_after_the_failure() => _retired.WrittenStates.Count.ShouldEqual(_writesBeforeReport);
    [Fact] void should_not_write_a_reference_to_the_deleted_plan() => _retired.WrittenStates[^1].HasStoredStepPlan.ShouldBeFalse();
    [Fact] void should_have_a_durable_running_state() => _retired.WrittenStates[^1].Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_complete_the_job() => _retired.AllStepsCompletedCount.ShouldEqual(0);
    [Fact] async Task should_run_the_next_activation_from_durable_running()
    {
        await ActivateFrom(_retired.WrittenStates[^1]);
        _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
        _job.CurrentState.HasStoredStepPlan.ShouldBeFalse();
    }
    [Fact] async Task should_resume_in_the_next_activation()
    {
        await ActivateFrom(_retired.WrittenStates[^1]);
        (await _job.Resume()).IsSuccess.ShouldBeTrue();
    }
}
