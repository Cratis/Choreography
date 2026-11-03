// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Contracts;

namespace Cratis.Choreography.Workflows.Adapters;

/// <summary>
/// Defines a performer whose work is a known, ordered list of operations - each run as its own job
/// step behind a stage barrier, each recorded before the next starts.
/// </summary>
/// <remarks>
/// Every operation but the last reports success by producing output the next operation receives; an
/// operation that does not succeed ends the node there, and the operations after it do nothing. The
/// last operation's outcome is the node's outcome.
/// </remarks>
public interface IStagedWorkflowNodePerformer : IWorkflowNodePerformer
{
    /// <summary>
    /// Gets the operations, in order. The last one completes the node.
    /// </summary>
    IEnumerable<WorkflowOperationName> Operations { get; }

    /// <summary>
    /// Performs one operation.
    /// </summary>
    /// <param name="invocation">The <see cref="WorkflowNodeInvocation"/>.</param>
    /// <param name="operation">The <see cref="WorkflowOperationName"/> to perform.</param>
    /// <param name="previous">What the previous operation produced - empty for the first.</param>
    /// <param name="cancellationToken">Cancelled when the attempt runs out of time.</param>
    /// <returns>The <see cref="WorkflowNodeOutcome"/>.</returns>
    Task<WorkflowNodeOutcome> PerformOperation(WorkflowNodeInvocation invocation, WorkflowOperationName operation, WorkflowData previous, CancellationToken cancellationToken);
}
