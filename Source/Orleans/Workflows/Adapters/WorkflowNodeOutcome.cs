// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Runs;

namespace Cratis.Orleans.Workflows.Adapters;

/// <summary>
/// What a performer reports about an attempt at a node.
/// </summary>
/// <param name="Status">The <see cref="WorkflowNodeOutcomeStatus"/>.</param>
/// <param name="Outlet">The <see cref="OutletName"/> to leave through, when it succeeded.</param>
/// <param name="Output">What it produced, as <see cref="WorkflowData"/>.</param>
/// <param name="Evidence">The evidence it recorded.</param>
/// <param name="Reason">Why it was skipped, failed or is ambiguous.</param>
public record WorkflowNodeOutcome(
    WorkflowNodeOutcomeStatus Status,
    OutletName Outlet,
    WorkflowData Output,
    IEnumerable<EvidenceReference> Evidence,
    WorkflowFailureReason Reason)
{
    /// <summary>
    /// Creates a successful outcome.
    /// </summary>
    /// <param name="outlet">The <see cref="OutletName"/> to leave through.</param>
    /// <param name="output">What it produced - empty when nothing.</param>
    /// <param name="evidence">The evidence it recorded.</param>
    /// <returns>The <see cref="WorkflowNodeOutcome"/>.</returns>
    public static WorkflowNodeOutcome Succeeded(OutletName outlet, WorkflowData? output = null, IEnumerable<EvidenceReference>? evidence = null) =>
        new(WorkflowNodeOutcomeStatus.Succeeded, outlet, output ?? WorkflowData.Empty, evidence ?? [], WorkflowFailureReason.None);

    /// <summary>
    /// Creates a skipped outcome.
    /// </summary>
    /// <param name="reason">Why it was skipped.</param>
    /// <returns>The <see cref="WorkflowNodeOutcome"/>.</returns>
    public static WorkflowNodeOutcome Skipped(WorkflowFailureReason reason) =>
        new(WorkflowNodeOutcomeStatus.Skipped, OutletName.None, WorkflowData.Empty, [], reason);

    /// <summary>
    /// Creates a failed outcome.
    /// </summary>
    /// <param name="reason">Why it failed.</param>
    /// <returns>The <see cref="WorkflowNodeOutcome"/>.</returns>
    public static WorkflowNodeOutcome Failed(WorkflowFailureReason reason) =>
        new(WorkflowNodeOutcomeStatus.Failed, OutletName.None, WorkflowData.Empty, [], reason);

    /// <summary>
    /// Creates an ambiguous outcome.
    /// </summary>
    /// <param name="reason">Why the effect is unknown.</param>
    /// <returns>The <see cref="WorkflowNodeOutcome"/>.</returns>
    public static WorkflowNodeOutcome Ambiguous(WorkflowFailureReason reason) =>
        new(WorkflowNodeOutcomeStatus.Ambiguous, OutletName.None, WorkflowData.Empty, [], reason);
}
