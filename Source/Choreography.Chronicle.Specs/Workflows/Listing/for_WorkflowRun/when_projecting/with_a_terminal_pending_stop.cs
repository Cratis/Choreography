// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Chronicle.Workflows.Listing.for_WorkflowRun.when_projecting;

public class with_a_terminal_pending_stop : Specification
{
    readonly WorkflowRunId _run = Guid.NewGuid().ToString();
    ReadModelScenario<WorkflowRun> _scenario = null!;

    void Establish() => _scenario = new();

    async Task Because() => await _scenario.Given.ForEventSource(_run).Events(
        given.an_admission(WorkflowId.New()), given.scheduled_in("job"), new WorkflowRunCancelled("stop"));

    [Fact] void should_remain_discoverable_for_stop_recovery() => _scenario.Instance.PendingJobStops.Single().ShouldEqual(new WorkflowJobReference("job"));
    [Fact] void should_still_be_terminal() => _scenario.Instance.Status.ShouldEqual(WorkflowRunStatus.Cancelled);
}
