// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

/// <summary>
/// The Running write commits without a plan reference, then throws. Orphan cleanup then deletes the plan, and a step report must
/// not write a reference to it again.
/// </summary>
public class and_the_running_write_commits_but_its_acknowledgement_is_lost : given.a_job_with_a_plan_store
{
    int _writesBeforeReport;

    async Task Establish()
    {
        _job.PersistBeforeFailingWrite = true;
        _job.RejectWrite = state => state.Status is JobStatus.Running && !state.HasStoredStepPlan && _writesBeforeReport == 0;
        _start = await _job.Start(new());
        await _planStore.RemoveForJob(_jobId);
        _writesBeforeReport = _job.WrittenStates.Count;
        _job.RejectWrite = null;
    }

    async Task Because() => await _job.OnStepSucceeded(_firstId, JobStepResult.Succeeded());

    [Fact] void should_continue_from_the_durable_running_state() => _job.CurrentState.Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_refer_to_a_stored_plan() => _job.CurrentState.HasStoredStepPlan.ShouldBeFalse();
    [Fact] void should_not_write_a_reference_to_the_deleted_plan() => _job.WrittenStates.Skip(_writesBeforeReport - 1).ShouldNotContain(state => state.HasStoredStepPlan);
    [Fact] void should_not_complete_the_job() => _job.AllStepsCompletedCount.ShouldEqual(0);
}
