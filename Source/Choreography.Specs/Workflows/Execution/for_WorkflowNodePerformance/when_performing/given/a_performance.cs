// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Execution.for_WorkflowNodePerformance.when_performing.given;

/// <summary>
/// One wave item, the run it reports to, and a performer the spec decides the behavior of.
/// </summary>
public class a_performance : Specification
{
    protected static readonly NodeExecutionKey Execution = NodeExecutionKey.RootOf("publish");
    protected static readonly WorkflowAttempt Attempt = new(2);
    protected static readonly CapabilityReference Capability = new("spec.publish", 1);

    protected IWorkflowRun Run = null!;
    protected IWorkflowNodePerformer? Performer;
    protected WorkflowWaveItem Item = null!;

    void Establish()
    {
        Run = Substitute.For<IWorkflowRun>();
        Run.TryStartInvocation(Execution, Attempt).Returns(true);
        Performer = Substitute.For<IWorkflowNodePerformer>();
        Performer.Capability.Returns(Capability);
        Item = new(
            "tenant/run-1",
            Execution.ToString(),
            Attempt.Value,
            Capability.Id.Value,
            Capability.Version.Value,
            "{}",
            "{}",
            "idempotency-1",
            1,
            WorkflowOperationName.None.Value,
            ["evidence-1"]);
    }

    protected Task PerformThrough(IWorkflowCapabilityGates gates) => new WorkflowNodePerformance(Run, Item, Execution, Attempt, Performer, gates).Perform(CancellationToken.None);

    protected Task Perform() => new WorkflowNodePerformance(Run, Item, Execution, Attempt, Performer).Perform(CancellationToken.None);
}
