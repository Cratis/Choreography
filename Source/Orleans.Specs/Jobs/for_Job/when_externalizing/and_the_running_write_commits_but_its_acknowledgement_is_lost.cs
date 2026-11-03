// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

/// <summary>
/// Running commits but its acknowledgement is lost. The retained plan still supports the restored in-memory reference.
/// </summary>
public class and_the_running_write_commits_but_its_acknowledgement_is_lost : given.a_job_with_a_plan_store
{
    void Establish()
    {
        _job.PersistBeforeFailingWrite = true;
        _job.RejectWrite = state => state.Status is JobStatus.Running;
    }

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_fail_the_start() => _start.IsSuccess.ShouldBeFalse();
    [Fact] void should_restore_the_plan_reference_in_memory() => _job.CurrentState.HasStoredStepPlan.ShouldBeTrue();
    [Fact] async Task should_keep_the_referenced_plan() => (await PlanIsStored()).ShouldBeTrue();
    [Fact] void should_have_committed_running() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_remove_the_plan() => _planRemovals.ShouldEqual(0);
}
