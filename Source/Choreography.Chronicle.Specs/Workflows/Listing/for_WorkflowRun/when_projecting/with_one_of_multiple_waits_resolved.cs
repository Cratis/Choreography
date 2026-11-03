// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Waits;

namespace Cratis.Choreography.Chronicle.Workflows.Listing.for_WorkflowRun.when_projecting;

public class with_one_of_multiple_waits_resolved : Specification
{
    readonly WorkflowRunId _run = Guid.NewGuid().ToString();
    ReadModelScenario<WorkflowRun> _scenario = null!;

    void Establish() => _scenario = new();

    async Task Because() => await _scenario.Given.ForEventSource(_run).Events(
        given.an_admission(WorkflowId.New()),
        new WorkflowWaitOpened("one", NodeExecutionKey.RootOf("one"), WaitKind.Signal, "correlation", WorkflowContractReference.Any, WaitDeadline.None),
        new WorkflowWaitOpened("two", NodeExecutionKey.RootOf("two"), WaitKind.Signal, "correlation", WorkflowContractReference.Any, WaitDeadline.None),
        new WorkflowWaitResolved("one", WellKnownOutlets.Resumed, WorkflowData.Empty, WorkflowActor.System));

    [Fact] void should_remain_waiting_for_the_other_wait() => _scenario.Instance.Status.ShouldEqual(WorkflowRunStatus.Waiting);
    [Fact] void should_keep_only_the_open_wait() => _scenario.Instance.Waits.Single().Wait.ShouldEqual(new WorkflowWaitId("two"));
}
