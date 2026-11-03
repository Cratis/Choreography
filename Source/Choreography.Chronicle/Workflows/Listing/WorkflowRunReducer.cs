// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Waits;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers;

// A reducer method receives the event it handles even when what it does depends on the run alone.
// The fact types carry no event type attribute because the core package does not know Chronicle; the adapter registers them
// explicitly with the Chronicle client (see RegisterEventSourcedWorkflows), which is what CHR0007 asks the attribute for.
#pragma warning disable IDE0060, CHR0007

namespace Cratis.Choreography.Chronicle.Workflows.Listing;

/// <summary>
/// Tracks all active executions and waits instead of treating the last event as the run's status.
/// </summary>
/// <remarks>
/// A reducer is required because status depends on concurrent execution states, not a constant setter.
/// </remarks>
public class WorkflowRunReducer : IReducerFor<WorkflowRun>
{
    /// <summary>Starts the run.</summary>
    /// <param name="event">The admission.</param>
    /// <param name="current">The current model.</param>
    /// <param name="context">The event context.</param>
    /// <returns>The new model.</returns>
    public WorkflowRun On(WorkflowRunAdmitted @event, WorkflowRun? current, EventContext context) => current ?? new(
        new WorkflowRunId(context.EventSourceId.Value),
        @event.Workflow,
        @event.Area,
        @event.Version,
        @event.Trigger.Subject,
        @event.Trigger.Correlation,
        WorkflowRunStatus.Running,
        context.Occurred,
        context.Occurred,
        null);

    /// <summary>Tracks scheduled work.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowNodeScheduled @event, WorkflowRun? current, EventContext context) => Track(current, @event.Execution, NodeExecutionStatus.Scheduled, context, @event.Job);

    /// <summary>Tracks success.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowNodeSucceeded @event, WorkflowRun? current, EventContext context) => Track(current, @event.Execution, NodeExecutionStatus.Succeeded, context);

    /// <summary>Tracks failure.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowNodeFailed @event, WorkflowRun? current, EventContext context) => Track(current, @event.Execution, NodeExecutionStatus.Failed, context);

    /// <summary>Tracks skipped work.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowNodeSkipped @event, WorkflowRun? current, EventContext context) => Track(current, @event.Execution, NodeExecutionStatus.Skipped, context);

    /// <summary>Tracks ambiguous work.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowNodeOutcomeAmbiguous @event, WorkflowRun? current, EventContext context) => Track(current, @event.Execution, NodeExecutionStatus.ReconciliationRequired, context);

    /// <summary>Tracks an open wait.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowWaitOpened @event, WorkflowRun? current, EventContext context) => current is null ? null : Track(
        current with { Waits = [.. current.Waits, new(@event.Wait, @event.Execution)] }, @event.Execution, NodeExecutionStatus.Waiting, context);

    /// <summary>Tracks a resolved wait.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowWaitResolved @event, WorkflowRun? current, EventContext context) => Close(current, @event.Wait, NodeExecutionStatus.Succeeded, context);

    /// <summary>Tracks a cancelled wait.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowWaitCancelled @event, WorkflowRun? current, EventContext context) => Close(current, @event.Wait, NodeExecutionStatus.Cancelled, context);

    /// <summary>Tracks a delivered stop.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowJobStopped @event, WorkflowRun? current, EventContext context) => Touch(current is null ? null : current with { PendingJobStops = [.. current.PendingJobStops.Where(job => job != @event.Job)] }, context);

    /// <summary>Tracks operation activity.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowNodeOperationCompleted @event, WorkflowRun? current, EventContext context) => Touch(current, context);

    /// <summary>Tracks a failed attempt - pending a retry until the next attempt is scheduled, or the node fails for good.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    /// <remarks>
    /// Whether a retry follows is the coordinator's decision; when none does, <see cref="WorkflowNodeFailed"/> is
    /// recorded in the same append and replaces this status.
    /// </remarks>
    public WorkflowRun? On(WorkflowNodeAttemptFailed @event, WorkflowRun? current, EventContext context) => Track(current, @event.Execution, NodeExecutionStatus.RetryPending, context);

    /// <summary>Tracks input activity.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowNodeInputPinned @event, WorkflowRun? current, EventContext context) => Touch(current, context);

    /// <summary>Tracks fan-out activity.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowBranchesFannedOut @event, WorkflowRun? current, EventContext context) => Touch(current, context);

    /// <summary>Tracks join activity.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowBranchesJoined @event, WorkflowRun? current, EventContext context) => Touch(current, context);

    /// <summary>Tracks reachability activity.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowNodesUnreachable @event, WorkflowRun? current, EventContext context) => Touch(current, context);

    /// <summary>Finishes the run.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowRunSucceeded @event, WorkflowRun? current, EventContext context) => Finish(current, WorkflowRunStatus.Succeeded, context);

    /// <summary>Finishes the run.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowRunFailed @event, WorkflowRun? current, EventContext context) => Finish(current, WorkflowRunStatus.Failed, context);

    /// <summary>Cancels the run, owing a stop to every job that still held scheduled work.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowRunCancelled @event, WorkflowRun? current, EventContext context) => Finish(
        current is null ? null : current with
        {
            PendingJobStops = [.. current.PendingJobStops
                .Concat(current.Executions.Where(item => item.Status == NodeExecutionStatus.Scheduled && item.Job is not null).Select(item => item.Job!))
                .Distinct()]
        },
        WorkflowRunStatus.Cancelled,
        context);

    /// <summary>Finishes the run.</summary>
    /// <param name="event">The event.</param>
    /// <param name="current">The model.</param>
    /// <param name="context">The context.</param>
    /// <returns>The updated model.</returns>
    public WorkflowRun? On(WorkflowRunNeedsReconciliation @event, WorkflowRun? current, EventContext context) => Finish(current, WorkflowRunStatus.ReconciliationRequired, context);

    static WorkflowRun? Track(WorkflowRun? current, NodeExecutionKey execution, NodeExecutionStatus status, EventContext context, WorkflowJobReference? job = null) => current is null ? null : Touch(
        current with { Executions = [.. current.Executions.Where(item => item.Execution != execution), new(execution, status) { Job = job }] }, context);

    static WorkflowRun? Close(WorkflowRun? current, WorkflowWaitId wait, NodeExecutionStatus status, EventContext context) =>
        current?.Waits.FirstOrDefault(item => item.Wait == wait) is not { } opened ? Touch(current, context)
        : Track(current with { Waits = [.. current.Waits.Where(item => item.Wait != wait)] }, opened.Execution, status, context);

    static WorkflowRun? Touch(WorkflowRun? current, EventContext context) => current is null ? null : current with
    {
        LastActivityAt = context.Occurred,
        Status = StatusOf(current)
    };

    static WorkflowRunStatus StatusOf(WorkflowRun current)
    {
        if (current.CompletedAt is not null)
        {
            return current.Status;
        }

        return current.Executions.Any(item => item.Status is NodeExecutionStatus.Scheduled or NodeExecutionStatus.RetryPending)
            || !current.Executions.Any(item => item.Status == NodeExecutionStatus.Waiting) ? WorkflowRunStatus.Running : WorkflowRunStatus.Waiting;
    }

    static WorkflowRun? Finish(WorkflowRun? current, WorkflowRunStatus status, EventContext context) =>
        current is null ? null : current with { Status = status, LastActivityAt = context.Occurred, CompletedAt = context.Occurred };
}
