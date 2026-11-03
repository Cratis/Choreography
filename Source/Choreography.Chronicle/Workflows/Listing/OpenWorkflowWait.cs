// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Waits;
using Cratis.Chronicle.Projections;

// The fact types are registered explicitly with the Chronicle client instead of carrying an event type attribute (CHR0002).
#pragma warning disable CHR0002

namespace Cratis.Choreography.Chronicle.Workflows.Listing;

/// <summary>
/// A wait that is open - waiting for a signal, a deadline or an approval.
/// </summary>
/// <param name="Id">The wait.</param>
/// <param name="Run">The run it belongs to.</param>
/// <param name="Execution">The execution that is waiting.</param>
/// <param name="Kind">What it waits for.</param>
/// <param name="Correlation">What a signal must be correlated with to resolve it.</param>
/// <param name="Signal">The contract of the signal it waits for.</param>
/// <param name="Deadline">When it expires, or <see cref="WaitDeadline.None"/>.</param>
public record OpenWorkflowWait(
    WorkflowWaitId Id,
    WorkflowRunId Run,
    NodeExecutionKey Execution,
    WaitKind Kind,
    WorkflowCorrelationKey Correlation,
    WorkflowContractReference Signal,
    WaitDeadline Deadline)
{
    /// <summary>
    /// Defines the projection that keeps the open waits - added when a wait opens, removed when it is resolved or cancelled.
    /// </summary>
    /// <param name="builder">The <see cref="IProjectionBuilderFor{TReadModel}"/>.</param>
    public static void Define(IProjectionBuilderFor<OpenWorkflowWait> builder) => builder
        .From<WorkflowWaitOpened>(from => from.UsingKey(@event => @event.Wait).Set(model => model.Run).ToEventSourceId())
        .RemovedWith<WorkflowWaitResolved>(removed => removed.UsingKey(@event => @event.Wait))
        .RemovedWith<WorkflowWaitCancelled>(removed => removed.UsingKey(@event => @event.Wait));
}
