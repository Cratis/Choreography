// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

/// <summary>
/// Crash window (a): the plan documents are written, the process dies before the job document is.
/// </summary>
public class and_the_plan_was_saved_but_the_job_never_wrote : given.a_job_with_a_plan_store
{
    void Establish() => _job.RejectWrite = state => state.Status is JobStatus.PreparingSteps or JobStatus.Failed;

    async Task Because() => _start = await _job.Start(new());

    [Fact] async Task should_leave_the_orphan_plan_for_cleanup() => (await PlanIsStored()).ShouldBeTrue();
    [Fact] void should_not_have_a_durable_job_referring_to_it() => _job.WrittenStates.ShouldNotContain(state => state.HasStoredStepPlan);
    [Fact] void should_not_have_removed_the_plan_on_the_way() => _planRemovals.ShouldEqual(0);
}
