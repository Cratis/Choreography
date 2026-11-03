// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Waits;

namespace Cratis.Choreography.Chronicle.Workflows.Listing.for_WorkflowRun.when_projecting;

public class with_an_action_and_a_wait : Specification
{
    readonly WorkflowRunId _run = Guid.NewGuid().ToString();
    ReadModelScenario<WorkflowRun> _scenario = null!;

    void Establish() => _scenario = new();

    async Task Because() => await _scenario.Given.ForEventSource(_run).Events(
        given.an_admission(WorkflowId.New()),
        new WorkflowNodeScheduled(NodeExecutionKey.RootOf("action"), WorkflowAttempt.First, "job", 1),
        new WorkflowWaitOpened("wait", NodeExecutionKey.RootOf("signal"), WaitKind.Signal, "correlation", WorkflowContractReference.Any, WaitDeadline.None));

    [Fact] void should_remain_running_while_the_action_is_scheduled() => _scenario.Instance.Status.ShouldEqual(WorkflowRunStatus.Running);
    [Fact] void should_track_both_executions() => _scenario.Instance.Executions.Count().ShouldEqual(2);
}
