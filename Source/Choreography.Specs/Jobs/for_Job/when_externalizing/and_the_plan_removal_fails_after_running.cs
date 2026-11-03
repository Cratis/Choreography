// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.for_Job.when_externalizing;

/// <summary>
/// Crash window (c): Running is durable and the plan is retained even if removal would fail.
/// </summary>
public class and_the_plan_removal_fails_after_running : given.a_job_with_a_plan_store
{
    void Establish() => _planRemovalFails = true;

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_not_attempt_removal_at_running() => _planRemovals.ShouldEqual(0);
    [Fact] void should_still_start() => _start.IsSuccess.ShouldBeTrue();
    [Fact] void should_be_running() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.Running);
    [Fact] void should_not_refer_to_the_plan_any_more() => _job.WrittenStates[^1].HasStoredStepPlan.ShouldBeFalse();
    [Fact] async Task should_retain_the_plan_until_terminal() => (await PlanIsStored()).ShouldBeTrue();
}
