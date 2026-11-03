// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Execution;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Monads;
using Moq;

namespace Cratis.Choreography.Workflows.Runs.for_WorkflowRunGrain.when_recovering_cancellation;

public class after_the_stop_failed : given.a_run
{
    Exception? _error;
    WorkflowRunSnapshot _snapshot = null!;

    async Task Establish()
    {
        await Grain.Admit(Definition, Trigger);
        Jobs.Setup(jobs => jobs.Stop(It.IsAny<JobId>())).ThrowsAsync(new IOException("Interrupted stop"));
        _error = await Cratis.Specifications.Catch.Exception(() => Grain.Cancel("stop"));
        Jobs.Setup(jobs => jobs.Stop(It.IsAny<JobId>())).ReturnsAsync(Result<StopJobError>.Success());
        await Reactivate();
    }

    async Task Because()
    {
        await Grain.Resume();
        await Grain.CompleteNode(NodeExecutionKey.RootOf("action"), WorkflowAttempt.First, WorkflowNodeOutcome.Succeeded("next", WorkflowData.Empty));
        _snapshot = await Grain.GetSnapshot();
    }

    [Fact] void should_surface_the_interruption() => _error.ShouldNotBeNull();
    [Fact] void should_record_no_stop_request() => History.Select(item => item.Fact.GetType().Name).ShouldNotContain("WorkflowJobStopRequested");
    [Fact] void should_acknowledge_recovered_delivery() => History.Select(item => item.Fact).OfType<WorkflowJobStopped>().Count().ShouldEqual(1);
    [Fact] void should_retry_the_stop() => Jobs.Verify(jobs => jobs.Stop(It.IsAny<JobId>()), Times.Exactly(2));
    [Fact] void should_remain_cancelled_after_the_late_outcome() => _snapshot.Status.ShouldEqual(WorkflowRunStatus.Cancelled);
    [Fact] void should_record_the_late_external_fact() => _snapshot.Executions.Single(item => item.Execution.Node == new WorkflowNodeId("action")).Status.ShouldEqual(NodeExecutionStatus.Succeeded);
    [Fact] async Task should_not_start_a_new_invocation() => (await Grain.TryStartInvocation(NodeExecutionKey.RootOf("action"), WorkflowAttempt.First)).ShouldBeFalse();
    [Fact] void should_not_start_a_new_wave() => Jobs.Verify(jobs => jobs.Start<IWorkflowWaveJob, WorkflowWaveRequest>(It.IsAny<JobId>(), It.IsAny<WorkflowWaveRequest>()), Times.Once());
}
