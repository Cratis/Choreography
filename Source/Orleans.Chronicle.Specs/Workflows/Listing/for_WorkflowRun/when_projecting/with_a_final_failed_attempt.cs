// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Chronicle.Workflows.Listing.for_WorkflowRun.when_projecting;

public class with_a_final_failed_attempt : Specification
{
    readonly WorkflowRunId _run = Guid.NewGuid().ToString();
    ReadModelScenario<WorkflowRun> _scenario = null!;

    void Establish() => _scenario = new();

    async Task Because() => await _scenario.Given.ForEventSource(_run).Events(
        given.an_admission(WorkflowId.New()),
        given.scheduled_in("job"),
        new WorkflowNodeAttemptFailed(NodeExecutionKey.RootOf("action"), WorkflowAttempt.First, WorkflowFailureKind.Error, "it went wrong"),
        new WorkflowNodeFailed(NodeExecutionKey.RootOf("action"), WorkflowAttempt.First, WorkflowFailureKind.Error, "it went wrong", OutletName.None));

    [Fact] void should_list_the_execution_as_failed() => _scenario.Instance.Executions.Single().Status.ShouldEqual(NodeExecutionStatus.Failed);
}
