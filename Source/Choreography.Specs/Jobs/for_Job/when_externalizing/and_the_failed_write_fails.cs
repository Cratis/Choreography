// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Jobs.for_Job.when_externalizing;

/// <summary>
/// Crash window (d): the Failed write fails, so the job document may still say it is preparing and needs its plan.
/// </summary>
public class and_the_failed_write_fails : given.a_job_with_a_plan_store
{
    void Establish() => _job.RejectWrite = state => state.Status is JobStatus.PreparingSteps or JobStatus.Failed;

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_fail_the_start() => _start.IsSuccess.ShouldBeFalse();
    [Fact] async Task should_keep_the_plan() => (await PlanIsStored()).ShouldBeTrue();
    [Fact] void should_not_try_to_remove_the_plan() => _planRemovals.ShouldEqual(0);
}
