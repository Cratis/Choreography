// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Chronicle.Workflows.Listing.for_WorkflowRun.when_projecting;

public class with_a_failed_attempt_left_to_retry : Specification
{
    readonly WorkflowRunId _run = Guid.NewGuid().ToString();
    ReadModelScenario<WorkflowRun> _scenario = null!;

    void Establish() => _scenario = new();

    async Task Because() => await _scenario.Given.ForEventSource(_run).Events(
        given.an_admission(WorkflowId.New()),
        given.scheduled_in("job"),
        new WorkflowNodeAttemptFailed(NodeExecutionKey.RootOf("action"), WorkflowAttempt.First, WorkflowFailureKind.Error, "it went wrong"));

    [Fact] void should_list_the_execution_as_pending_a_retry() => _scenario.Instance.Executions.Single().Status.ShouldEqual(NodeExecutionStatus.RetryPending);
    [Fact] void should_keep_the_run_running() => _scenario.Instance.Status.ShouldEqual(WorkflowRunStatus.Running);
}
