// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Workflows.Execution;
using Moq;

namespace Cratis.Orleans.Workflows.Runs.for_WorkflowRunGrain.when_executing_wave_steps;

public class after_a_launch_interruption : given.a_run_with_wave_steps
{
    Exception? _error;
    WorkflowRunSnapshot _snapshot = null!;

    async Task Establish()
    {
        Jobs.Setup(jobs => jobs.Start<IWorkflowWaveJob, WorkflowWaveRequest>(It.IsAny<JobId>(), It.IsAny<WorkflowWaveRequest>())).ThrowsAsync(new IOException("Launch interrupted after append"));
        _error = await Cratis.Specifications.Catch.Exception(() => Grain.Admit(Definition, Trigger));
        RecordWaves();
        await Reactivate();
    }

    async Task Because()
    {
        await Grain.Resume();
        await DrainWaves();
        _snapshot = await Grain.GetSnapshot();
    }

    [Fact] void should_surface_the_launch_failure() => _error.ShouldNotBeNull();
    [Fact] void should_keep_one_admission() => History.Select(item => item.Fact).OfType<WorkflowRunAdmitted>().Count().ShouldEqual(1);
    [Fact] void should_recover_in_the_next_wave() => History.Select(item => item.Fact).OfType<WorkflowNodeScheduled>().Last().Wave.Value.ShouldEqual(2);
    [Fact] void should_perform_the_recovered_work_once() => Invocations.Count.ShouldEqual(1);
    [Fact] void should_finish_from_the_recovered_step() => _snapshot.Status.ShouldEqual(WorkflowRunStatus.Succeeded);
}
