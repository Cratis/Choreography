// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Definitions;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Runs;
using Cratis.Orleans.Workflows.Triggers;
using Cratis.Orleans.Workflows.Waits;

namespace Cratis.Orleans.Chronicle.Workflows;

/// <summary>
/// One instance of every fact a run records, with every member given a value that is not a default.
/// </summary>
public static class WorkflowFactSamples
{
    static readonly DateTimeOffset _moment = new(2026, 3, 1, 9, 30, 0, TimeSpan.Zero);
    static readonly NodeExecutionKey _execution = new("publish", "each//2:one", 2);

    public static IReadOnlyList<object> All { get; } =
    [
        new WorkflowRunAdmitted(
            "workflow-1",
            "area-1",
            3,
            "catalog-7",
            [new CapabilityPin(new CapabilityReference("capability-1", 2), "provider-9")],
            new WorkflowGraph(
                [new WorkflowNode("trigger", WorkflowNodeKind.Trigger, new CapabilityReference("trigger", 1), "{\"a\":1}", [WellKnownOutlets.Next], new WorkflowNodePolicy(3, TimeSpan.FromMinutes(2), TimeSpan.FromSeconds(5), 4))],
                [new WorkflowEdge("trigger", WellKnownOutlets.Next, "publish")]),
            new WorkflowTrigger(
                WorkflowTriggerKind.Signal,
                "source-1",
                "dedup-1",
                new WorkflowSubjectReference("subject-kind", "subject-1"),
                new WorkflowContractReference("contract-1", 2),
                "{\"payload\":true}",
                ["evidence-1"],
                "correlation-1",
                "causation-1",
                "someone",
                _moment)),
        new WorkflowNodeScheduled(_execution, 2, "job-1", 5),
        new WorkflowNodeOperationCompleted(_execution, 2, "register", "{\"done\":1}"),
        new WorkflowNodeSucceeded(_execution, 2, "next", "{\"out\":1}", ["evidence-2"]),
        new WorkflowNodeSkipped(_execution, "nothing to do"),
        new WorkflowNodeAttemptFailed(_execution, 2, WorkflowFailureKind.TimedOut, "too slow"),
        new WorkflowNodeFailed(_execution, 2, WorkflowFailureKind.Error, "it went wrong", "failed"),
        new WorkflowNodeOutcomeAmbiguous(_execution, 2, "unknown outcome"),
        new WorkflowNodesUnreachable([_execution]),
        new WorkflowBranchesFannedOut(_execution, [new WorkflowBranch("branch-1", "{\"item\":1}")]),
        new WorkflowBranchesJoined(_execution, "completed", "{\"results\":[]}"),
        new WorkflowWaitOpened("wait-1", _execution, WaitKind.Approval, "correlation-1", new WorkflowContractReference("contract-1", 2), new WaitDeadline(_moment)),
        new WorkflowWaitResolved("wait-1", "approved", "{\"by\":\"someone\"}", "someone"),
        new WorkflowWaitCancelled("wait-1"),
        new WorkflowRunSucceeded(),
        new WorkflowRunFailed("it failed"),
        new WorkflowRunCancelled("called off"),
        new WorkflowRunNeedsReconciliation(),
        new WorkflowNodeInputPinned(_execution, "{\"input\":1}"),
        new WorkflowJobStopped("job-1")
    ];
}
