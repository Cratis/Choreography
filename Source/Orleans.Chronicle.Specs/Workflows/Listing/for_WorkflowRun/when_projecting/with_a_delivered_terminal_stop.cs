// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows;
using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Chronicle.Workflows.Listing.for_WorkflowRun.when_projecting;

public class with_a_delivered_terminal_stop : Specification
{
    readonly WorkflowRunId _run = Guid.NewGuid().ToString();
    ReadModelScenario<WorkflowRun> _scenario = null!;

    void Establish() => _scenario = new();

    async Task Because() => await _scenario.Given.ForEventSource(_run).Events(
        given.an_admission(WorkflowId.New()), given.scheduled_in("job"), new WorkflowRunCancelled("stop"), new WorkflowJobStopped("job"));

    [Fact] void should_clear_the_pending_stop() => _scenario.Instance.PendingJobStops.ShouldBeEmpty();
    [Fact] void should_keep_the_terminal_status() => _scenario.Instance.Status.ShouldEqual(WorkflowRunStatus.Cancelled);
}
