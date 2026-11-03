// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Journal;
using Cratis.Choreography.Workflows.Runs.Coordination;

namespace Cratis.Choreography.Workflows.Runs.for_WorkflowRunGrain.when_activated_after_a_restart;

public class and_a_wave_job_is_gone : given.a_run
{
    void Establish()
    {
        var decision = WorkflowCoordinator.Admit(Key, new WorkflowRunState(Key.Run), Definition, Trigger, DateTimeOffset.UtcNow);
        foreach (var @event in decision.Events)
        {
            History.Add(new WorkflowRecordedFact(@event, DateTimeOffset.UtcNow));
        }
    }

    Task Because() => Grain.Resume();

    [Fact] void should_reschedule_the_execution_in_a_new_wave() => History.Select(item => item.Fact).OfType<WorkflowNodeScheduled>().Last().Wave.Value.ShouldEqual(2);
    [Fact] void should_keep_the_attempt() => History.Select(item => item.Fact).OfType<WorkflowNodeScheduled>().Last().Attempt.ShouldEqual(WorkflowAttempt.First);
    [Fact] void should_keep_one_admission() => History.Select(item => item.Fact).OfType<WorkflowRunAdmitted>().Count().ShouldEqual(1);
}
