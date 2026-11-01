// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Adapters;

/// <summary>
/// Defines what performs the nodes of one capability. Every implementation is discovered.
/// </summary>
/// <remarks>
/// A performer may be asked to perform the same node execution more than once - a restart or a retry
/// does that - so it deduplicates external effects on <see cref="WorkflowNodeInvocation.IdempotencyKey"/>.
/// A performer that cannot tell whether its effect happened reports
/// <see cref="WorkflowNodeOutcomeStatus.Ambiguous"/> rather than guessing.
/// </remarks>
public interface IWorkflowNodePerformer
{
    /// <summary>
    /// Gets the <see cref="CapabilityReference"/> it performs.
    /// </summary>
    CapabilityReference Capability { get; }

    /// <summary>
    /// Performs a node.
    /// </summary>
    /// <param name="invocation">The <see cref="WorkflowNodeInvocation"/>.</param>
    /// <param name="cancellationToken">Cancelled when the attempt runs out of time.</param>
    /// <returns>The <see cref="WorkflowNodeOutcome"/>.</returns>
    Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken);
}
