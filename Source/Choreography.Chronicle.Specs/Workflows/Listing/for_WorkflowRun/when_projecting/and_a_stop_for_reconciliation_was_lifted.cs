// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Chronicle.Workflows.Listing.for_WorkflowRun.when_projecting;

public class and_a_stop_for_reconciliation_was_lifted : Specification
{
    readonly WorkflowRunId _run = Guid.NewGuid().ToString();
    ReadModelScenario<WorkflowRun> _scenario = null!;

    void Establish() => _scenario = new();

    async Task Because() => await _scenario.Given.ForEventSource(_run).Events(
        given.an_admission(WorkflowId.New()),
        new WorkflowRunNeedsReconciliation(),
        new WorkflowNodeReconciled(NodeExecutionKey.RootOf("action"), WorkflowAttempt.First, "operator"));

    [Fact] void should_run_again() => _scenario.Instance.Status.ShouldEqual(WorkflowRunStatus.Running);
    [Fact] void should_no_longer_be_completed() => _scenario.Instance.CompletedAt.ShouldBeNull();
}
