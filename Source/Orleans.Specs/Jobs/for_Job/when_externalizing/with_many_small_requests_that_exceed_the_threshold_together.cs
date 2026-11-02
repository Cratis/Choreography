// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

/// <summary>
/// The threshold is measured on the whole serialized plan, with identities, type names and stages, not on the request bodies alone.
/// </summary>
public class with_many_small_requests_that_exceed_the_threshold_together : given.a_job_with_a_plan_store
{
    bool _requestsAloneFit;

    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 1, MaxInlineStepPlanSize = 500 };

    async Task Because()
    {
        _start = await _job.Start(new());
        _requestsAloneFit = _job.WrittenStates.First(state => state.Status is JobStatus.PreparingSteps).HasStoredStepPlan;
    }

    [Fact] void should_have_externalized_the_plan() => _requestsAloneFit.ShouldBeTrue();
    [Fact] void should_not_keep_the_plan_inline() => _job.WrittenStates.First(state => state.Status is JobStatus.PreparingSteps).StepPlan.ShouldBeNull();
}
