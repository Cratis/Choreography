// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

/// <summary>
/// The Running write is lost: the durable job still refers to its plan, which therefore must stay.
/// </summary>
public class and_the_running_write_is_not_committed : given.a_job_with_a_plan_store
{
    void Establish() => _job.RejectWrite = state => state.Status is JobStatus.Running;

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_fail_the_start() => _start.IsSuccess.ShouldBeFalse();
    [Fact] void should_still_refer_to_the_plan() => _job.CurrentState.HasStoredStepPlan.ShouldBeTrue();
    [Fact] async Task should_keep_the_plan() => (await PlanIsStored()).ShouldBeTrue();
}
