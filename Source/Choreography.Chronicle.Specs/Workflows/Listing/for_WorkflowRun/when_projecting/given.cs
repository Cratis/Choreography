// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Triggers;

namespace Cratis.Choreography.Chronicle.Workflows.Listing.for_WorkflowRun.when_projecting;

/// <summary>
/// The admission every projected run starts from.
/// </summary>
public static class given
{
    /// <summary>
    /// Creates an admission.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <returns>The <see cref="WorkflowRunAdmitted"/>.</returns>
    public static WorkflowRunAdmitted an_admission(WorkflowId workflow) => new(
        workflow,
        "product",
        3,
        "catalog-7",
        [],
        new WorkflowGraph([], []),
        new WorkflowTrigger(
            WorkflowTriggerKind.Manual,
            "spec",
            "dedup-1",
            new("issue", "issue-42"),
            WorkflowContractReference.Any,
            WorkflowData.Empty,
            [],
            "correlation-42",
            WorkflowCausationKey.None,
            WorkflowActor.System,
            DateTimeOffset.UtcNow));

    /// <summary>
    /// Creates a scheduling of an action into a wave job.
    /// </summary>
    /// <param name="job">The wave job.</param>
    /// <returns>The <see cref="WorkflowNodeScheduled"/>.</returns>
    public static WorkflowNodeScheduled scheduled_in(WorkflowJobReference job) => new(NodeExecutionKey.RootOf("action"), WorkflowAttempt.First, job, 1);
}
