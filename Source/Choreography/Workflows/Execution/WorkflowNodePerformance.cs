// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;

namespace Cratis.Choreography.Workflows.Execution;

/// <summary>
/// Performs a wave item and reports its outcome. Reporting failures propagate to the job step, never
/// become business failures; only exceptions thrown by the performer are translated into outcomes.
/// </summary>
/// <param name="run">The run to report to.</param>
/// <param name="item">The wave item.</param>
/// <param name="execution">The execution.</param>
/// <param name="attempt">The attempt.</param>
/// <param name="performer">The performer, or null when none exists.</param>
/// <param name="gates">The <see cref="IWorkflowCapabilityGates"/> a performer call waits for room in, or null for none.</param>
public class WorkflowNodePerformance(IWorkflowRun run, WorkflowWaveItem item, NodeExecutionKey execution, WorkflowAttempt attempt, IWorkflowNodePerformer? performer, IWorkflowCapabilityGates? gates = null)
{
    WorkflowNodeInvocation Invocation => new(
        WorkflowRunKey.Parse(item.Run),
        execution,
        attempt,
        new(item.IdempotencyKey),
        new(item.Configuration),
        new(item.Input),
        [.. item.Evidence.Select(evidence => new EvidenceReference(evidence))]);

    /// <summary>
    /// Performs the item, bounded by its timeout even if the performer ignores cancellation.
    /// </summary>
    /// <param name="cancellationToken">Cancelled when the job is stopped.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Perform(CancellationToken cancellationToken)
    {
        if (!await run.TryStartInvocation(execution, attempt))
        {
            return;
        }

        if (performer is null)
        {
            await run.FailAttempt(execution, attempt, WorkflowFailureKind.PerformerMissing, $"No performer is registered for '{item.Capability}@{item.CapabilityVersion}'");
            return;
        }

        if (performer is IStagedWorkflowNodePerformer staged && !string.IsNullOrEmpty(item.Operation))
        {
            await PerformOperation(staged, cancellationToken);
            return;
        }

        var (outcome, timedOut) = await Invoke(token => performer.Perform(Invocation, token), cancellationToken);
        await Report(outcome, timedOut);
    }

    async Task PerformOperation(IStagedWorkflowNodePerformer staged, CancellationToken cancellationToken)
    {
        var operations = staged.Operations.ToList();
        var index = operations.FindIndex(operation => operation.Value == item.Operation);
        if (index < 0)
        {
            await run.CompleteNode(execution, attempt, WorkflowNodeOutcome.Failed($"The performer no longer has an operation called '{item.Operation}'"));
            return;
        }

        var previous = WorkflowData.Empty;
        if (index > 0)
        {
            previous = await run.GetOperationOutput(execution, attempt, operations[index - 1]);
            if (previous.IsEmpty)
            {
                return;
            }
        }

        var (outcome, timedOut) = await Invoke(token => staged.PerformOperation(Invocation, operations[index], previous, token), cancellationToken);
        var isLastOperation = index == operations.Count - 1;
        if (CompletesOperationOnly(outcome, timedOut, isLastOperation))
        {
            await CompleteOperation(operations[index], outcome);
            return;
        }

        await Report(outcome, timedOut);
    }

    bool CompletesOperationOnly(WorkflowNodeOutcome outcome, bool timedOut, bool isLastOperation) =>
        !timedOut && !isLastOperation && outcome.Status == WorkflowNodeOutcomeStatus.Succeeded;

    Task CompleteOperation(WorkflowOperationName operation, WorkflowNodeOutcome outcome)
    {
        if (outcome.Output.ExceedsLimit || !outcome.Output.IsValidJson)
        {
            return run.FailAttempt(execution, attempt, WorkflowFailureKind.InvalidOutput, "The operation output is not bounded valid JSON");
        }

        return run.CompleteOperation(execution, attempt, operation, outcome.Output.IsEmpty ? WorkflowNodeStep.CompletedWithoutOutput : outcome.Output);
    }

    async Task<(WorkflowNodeOutcome Outcome, bool TimedOut)> Invoke(Func<CancellationToken, Task<WorkflowNodeOutcome>> perform, CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(Math.Max(1, item.TimeoutSeconds));
        using var attemptCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        attemptCancellation.CancelAfter(timeout);
        try
        {
            using var lease = gates is null ? null : await gates.Enter(new CapabilityReference(item.Capability, item.CapabilityVersion), attemptCancellation.Token);
            return (await perform(attemptCancellation.Token).WaitAsync(timeout, cancellationToken), false);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested && exception is TimeoutException or OperationCanceledException)
        {
            return (WorkflowNodeOutcome.Failed($"The attempt ran longer than {timeout}"), true);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            return (WorkflowNodeOutcome.Failed(exception.Message), false);
        }
    }

    Task Report(WorkflowNodeOutcome outcome, bool timedOut) => timedOut
        ? run.FailAttempt(execution, attempt, WorkflowFailureKind.TimedOut, outcome.Reason)
        : run.CompleteNode(execution, attempt, outcome);
}
