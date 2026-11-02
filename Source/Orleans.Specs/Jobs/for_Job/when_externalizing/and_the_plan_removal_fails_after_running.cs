// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

/// <summary>
/// Crash window (c): Running is durable, the plan removal never happens.
/// </summary>
public class and_the_plan_removal_fails_after_running : given.a_job_with_a_plan_store
{
    void Establish() => _planRemovalFails = true;

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_still_start() => _start.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_running() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_refer_to_the_plan_any_more() => _job.WrittenStates[^1].HasStoredStepPlan.ShouldBeFalse();
    [Fact] async Task should_leave_the_orphan_plan_for_cleanup() => (await PlanIsStored()).ShouldBeTrue();
}
