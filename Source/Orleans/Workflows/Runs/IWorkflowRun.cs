// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Adapters;
using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Definitions;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Triggers;
using Cratis.Orleans.Workflows.Waits;

namespace Cratis.Orleans.Workflows.Runs;

/// <summary>
/// Defines one run of a workflow. Keyed by <see cref="WorkflowRunKey"/> text.
/// </summary>
/// <remarks>
/// The run's journal of facts is its only state: the grain rebuilds itself from it on activation, and every
/// call is answered by recording facts on it. Every call is safe to repeat - what no longer applies
/// records nothing.
/// </remarks>
public interface IWorkflowRun : IGrainWithStringKey
{
    /// <summary>
    /// Admits the run - or answers with the run already admitted for the same trigger.
    /// </summary>
    /// <param name="definition">The <see cref="WorkflowDefinitionSnapshot"/> to pin.</param>
    /// <param name="trigger">The <see cref="WorkflowTrigger"/>.</param>
    /// <returns>The <see cref="WorkflowAdmission"/>.</returns>
    Task<WorkflowAdmission> Admit(WorkflowDefinitionSnapshot definition, WorkflowTrigger trigger);

    /// <summary>
    /// Linearizes the start of an invocation against cancellation. A permitted invocation may report
    /// its late external outcome; queued invocations after cancellation may not start.
    /// </summary>
    /// <param name="execution">The execution.</param>
    /// <param name="attempt">The attempt.</param>
    /// <returns>Whether this scheduled attempt may start.</returns>
    Task<bool> TryStartInvocation(NodeExecutionKey execution, WorkflowAttempt attempt);

    /// <summary>
    /// Reports the outcome of an attempt at a node.
    /// </summary>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/>.</param>
    /// <param name="outcome">The <see cref="WorkflowNodeOutcome"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task CompleteNode(NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowNodeOutcome outcome);

    /// <summary>
    /// Reports that an attempt failed in a way its performer could not report - it timed out, or there is no performer.
    /// </summary>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/>.</param>
    /// <param name="kind">The <see cref="WorkflowFailureKind"/>.</param>
    /// <param name="reason">The <see cref="WorkflowFailureReason"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task FailAttempt(NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowFailureKind kind, WorkflowFailureReason reason);

    /// <summary>
    /// Reports that one operation of a staged node completed.
    /// </summary>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/>.</param>
    /// <param name="operation">The <see cref="WorkflowOperationName"/>.</param>
    /// <param name="output">What it produced.</param>
    /// <returns>Awaitable task.</returns>
    Task CompleteOperation(NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowOperationName operation, WorkflowData output);

    /// <summary>
    /// Gets what an operation of a staged node produced.
    /// </summary>
    /// <param name="execution">The <see cref="NodeExecutionKey"/>.</param>
    /// <param name="attempt">The <see cref="WorkflowAttempt"/>.</param>
    /// <param name="operation">The <see cref="WorkflowOperationName"/>.</param>
    /// <returns>The output - <see cref="WorkflowData.Empty"/> when the operation has not completed.</returns>
    Task<WorkflowData> GetOperationOutput(NodeExecutionKey execution, WorkflowAttempt attempt, WorkflowOperationName operation);

    /// <summary>
    /// Resolves an open wait.
    /// </summary>
    /// <param name="wait">The <see cref="WorkflowWaitId"/>.</param>
    /// <param name="outlet">The <see cref="OutletName"/> to leave through.</param>
    /// <param name="payload">What came with the resolution.</param>
    /// <param name="actor">The <see cref="WorkflowActor"/> who resolved it.</param>
    /// <returns>Awaitable task.</returns>
    Task ResolveWait(WorkflowWaitId wait, OutletName outlet, WorkflowData payload, WorkflowActor actor);

    /// <summary>
    /// Expires every wait whose deadline has passed.
    /// </summary>
    /// <param name="now">The time.</param>
    /// <returns>Awaitable task.</returns>
    Task ExpireDueWaits(DateTimeOffset now);

    /// <summary>
    /// Starts every retry whose time has come.
    /// </summary>
    /// <param name="now">The time.</param>
    /// <returns>Awaitable task.</returns>
    Task StartDueRetries(DateTimeOffset now);

    /// <summary>
    /// Takes the run forward when time has made something due - expires waits whose deadline has passed, starts retries whose
    /// time has come and, for a run that has been idle too long or owes a stop, takes it forward again from its recorded history.
    /// </summary>
    /// <param name="now">The time.</param>
    /// <returns>Awaitable task.</returns>
    Task Tend(DateTimeOffset now);

    /// <summary>
    /// Takes the run forward again from its recorded history, rescheduling scheduled work whose wave job is gone.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Resume();

    /// <summary>
    /// Cancels the run.
    /// </summary>
    /// <param name="reason">The <see cref="WorkflowFailureReason"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Cancel(WorkflowFailureReason reason);

    /// <summary>
    /// Gets where the run stands.
    /// </summary>
    /// <returns>The <see cref="WorkflowRunSnapshot"/>.</returns>
    Task<WorkflowRunSnapshot> GetSnapshot();
}
