// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Waits;

namespace Cratis.Choreography.Workflows.Adapters;

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
    /// Gets the <see cref="WorkflowCompletionKey"/> a suspended node is completed by.
    /// </summary>
    public WorkflowCompletionKey CompletionKey { get; init; } = WorkflowCompletionKey.None;

    /// <summary>
    /// Gets how long a suspended node waits before it expires through the <c lang="csharp">expired</c> outlet - zero for never.
    /// </summary>
    public TimeSpan ExpiresAfter { get; init; } = TimeSpan.Zero;

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
    /// Creates a suspended outcome: the performer accepted the work and answers later. The node leaves the worker and waits,
    /// like a <c lang="csharp">wait</c>, until it is completed through <paramref name="key"/> or runs out of time.
    /// </summary>
    /// <param name="key">The <see cref="WorkflowCompletionKey"/> the answer arrives under - for an extension invocation, its identity.</param>
    /// <param name="expiresAfter">How long to wait before the node expires - zero for never.</param>
    /// <returns>The <see cref="WorkflowNodeOutcome"/>.</returns>
    public static WorkflowNodeOutcome Suspended(WorkflowCompletionKey key, TimeSpan expiresAfter = default) =>
        new(WorkflowNodeOutcomeStatus.Suspended, OutletName.None, WorkflowData.Empty, [], WorkflowFailureReason.None)
        {
            CompletionKey = key,
            ExpiresAfter = expiresAfter
        };

    /// <summary>
    /// Creates an ambiguous outcome.
    /// </summary>
    /// <param name="reason">Why the effect is unknown.</param>
    /// <returns>The <see cref="WorkflowNodeOutcome"/>.</returns>
    public static WorkflowNodeOutcome Ambiguous(WorkflowFailureReason reason) =>
        new(WorkflowNodeOutcomeStatus.Ambiguous, OutletName.None, WorkflowData.Empty, [], reason);
}
