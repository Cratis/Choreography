// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

public class and_the_stop_is_finalized : given.a_job_with_a_plan_store
{
    bool _retainedUntilFinalized;

    async Task Establish()
    {
        await _job.Start(new());
        await _job.Stop();
        _retainedUntilFinalized = await PlanIsStored();
    }

    async Task Because()
    {
        await _job.OnStepStopped(_firstId, Stopped);
        await _job.OnStepStopped(_secondId, Stopped);
    }

    [Fact] void should_keep_the_plan_while_stopping() => _retainedUntilFinalized.ShouldBeTrue();
    [Fact] void should_durably_finalize_the_stop() => _job.WrittenStates[^1].StopFinalized.ShouldBeTrue();
    [Fact] void should_keep_the_stopped_job() => _job.ClearAttempts.ShouldEqual(0);
    [Fact] async Task should_remove_the_plan_after_finalizing() => (await _planStore.GetJobsWithPlans()).AsT0.ShouldBeEmpty();
}
