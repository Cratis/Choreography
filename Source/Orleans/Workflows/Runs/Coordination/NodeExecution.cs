// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Runs.Coordination;

/// <summary>
/// What a run's history says about one node execution.
/// </summary>
/// <param name="key">The <see cref="NodeExecutionKey"/>.</param>
public class NodeExecution(NodeExecutionKey key)
{
    /// <summary>Gets the <see cref="NodeExecutionKey"/>.</summary>
    public NodeExecutionKey Key { get; } = key;

    /// <summary>Gets or sets the <see cref="NodeExecutionStatus"/>.</summary>
    public NodeExecutionStatus Status { get; set; } = NodeExecutionStatus.Pending;

    /// <summary>Gets or sets the current <see cref="WorkflowAttempt"/>.</summary>
    public WorkflowAttempt Attempt { get; set; } = WorkflowAttempt.First;

    /// <summary>Gets or sets the attempt a pending retry will make.</summary>
    public WorkflowAttempt NextAttempt { get; set; } = WorkflowAttempt.First;

    /// <summary>Gets or sets the earliest a pending retry may start.</summary>
    public DateTimeOffset NotBefore { get; set; } = DateTimeOffset.MinValue;

    /// <summary>Gets or sets the wave job the current attempt was scheduled into.</summary>
    public WorkflowJobReference Job { get; set; } = new(string.Empty);

    /// <summary>Gets or sets the wave the current attempt was scheduled into.</summary>
    public int Wave { get; set; }

    /// <summary>Gets or sets the outlet the execution left through.</summary>
    public OutletName Outlet { get; set; } = OutletName.None;

    /// <summary>Gets or sets what the execution produced.</summary>
    public WorkflowData Output { get; set; } = WorkflowData.Empty;

    /// <summary>Gets or sets the evidence the execution recorded.</summary>
    public IEnumerable<EvidenceReference> Evidence { get; set; } = [];

    /// <summary>Gets or sets how the last failed attempt failed.</summary>
    public WorkflowFailureKind FailureKind { get; set; }

    /// <summary>Gets or sets why the execution failed, was skipped, or is ambiguous.</summary>
    public WorkflowFailureReason Reason { get; set; } = WorkflowFailureReason.None;

    /// <summary>
    /// Gets whether the execution is still going - scheduled, waiting, or waiting for a retry.
    /// </summary>
    public bool IsInFlight => Status is NodeExecutionStatus.Scheduled or NodeExecutionStatus.Waiting or NodeExecutionStatus.RetryPending;

    /// <summary>
    /// Creates an independent copy.
    /// </summary>
    /// <returns>The copy.</returns>
    public NodeExecution Clone() => (NodeExecution)MemberwiseClone();
}
