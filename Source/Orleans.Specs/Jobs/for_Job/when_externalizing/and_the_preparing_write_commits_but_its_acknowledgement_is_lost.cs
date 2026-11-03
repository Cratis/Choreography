// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Jobs.for_Job.when_externalizing;

/// <summary>
/// Crash window (b): the PreparingSteps write commits, then throws. The job document may refer to the plan, so it must survive until a durable end.
/// </summary>
public class and_the_preparing_write_commits_but_its_acknowledgement_is_lost : given.a_job_with_a_plan_store
{
    int _removalsWhenFailedWasWritten = -1;

    void Establish()
    {
        _job.PersistBeforeFailingWrite = true;
        _job.RejectWrite = state => state.Status is JobStatus.PreparingSteps;
        _job.AfterWrite = () =>
        {
            if (_job.CurrentState.Status is JobStatus.Failed)
            {
                _removalsWhenFailedWasWritten = _planRemovals;
            }
            return Task.CompletedTask;
        };
    }

    async Task Because() => _start = await _job.Start(new());

    [Fact] void should_fail_the_start() => _start.IsSuccess.ShouldBeFalse();
    [Fact] void should_have_committed_the_preparing_state() => _job.WrittenStates.ShouldContain(state => state.Status is JobStatus.PreparingSteps && state.HasStoredStepPlan);
    [Fact] void should_durably_fail_the_job() => _job.WrittenStates[^1].Status.ShouldEqual(JobStatus.Failed);
    [Fact] void should_not_remove_the_plan_before_the_failed_write() => _removalsWhenFailedWasWritten.ShouldEqual(0);
    [Fact] async Task should_remove_the_plan_after_the_failed_write() => (await _planStore.GetJobsWithPlans()).AsT0.ShouldBeEmpty();
}
