// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Scopes;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Choreography.Workflows.Execution;

/// <summary>
/// Performs one node execution attempt - or one operation of it - and reports back to the run.
/// </summary>
/// <param name="state"><see cref="IPersistentState{TState}"/> for the step's own bookkeeping.</param>
/// <param name="throttle">The <see cref="IJobStepThrottle"/> limiting parallel execution.</param>
/// <param name="logger">The logger.</param>
/// <param name="scopeFactory">The scope the performer is resolved from, in the scope the step belongs to.</param>
/// <param name="scopes">The <see cref="IWorkflowExecutionScopes"/> that establish the context work for a scope runs in.</param>
/// <remarks>
/// A business failure - the performer failing, throwing, running out of time, or not existing - is a fact
/// reported to the run, and the step itself succeeds. The step fails only when the report could not reach
/// the run, so the job's own failure handling covers exactly the case where the outcome would be lost.
/// </remarks>
[GrainType("cratis-orleans-workflow-node-step")]
public class WorkflowNodeStep(
    [PersistentState(nameof(WorkflowNodeStepState), WellKnownGrainStorageProviders.JobSteps)] IPersistentState<WorkflowNodeStepState> state,
    IJobStepThrottle throttle,
    ILogger<WorkflowNodeStep> logger,
    IServiceScopeFactory scopeFactory,
    IWorkflowExecutionScopes scopes)
    : JobStep<WorkflowWaveItem, object, WorkflowNodeStepState>(state, throttle, logger), IWorkflowNodeStep
{
    /// <summary>
    /// The output an intermediate operation is recorded with when it produced none - so the next
    /// operation can tell it completed.
    /// </summary>
    public static readonly WorkflowData CompletedWithoutOutput = new("{\"completed\":true}");

    /// <inheritdoc/>
    protected override Task<Result<PrepareJobStepError>> PrepareStep(WorkflowWaveItem request) => Task.FromResult(Result<PrepareJobStepError>.Success());

    /// <inheritdoc/>
    protected override ValueTask InitializeState(WorkflowWaveItem request)
    {
        State.Item = request;
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task<Catch<JobStepResult>> PerformStep(WorkflowNodeStepState currentState, CancellationToken cancellationToken)
    {
        if (currentState.Item is not { } item)
        {
            return JobStepResult.Failed("The step has nothing to perform");
        }

        _ = this.GetPrimaryKey(out var key);
        var jobStepKey = (JobStepKey)key!;
        using var scopeContext = scopes.Enter(scopes.ScopeOf(new JobsManagerKey(jobStepKey.Scope, jobStepKey.Namespace)));
        await using var scope = scopeFactory.CreateAsyncScope();
        using var nodeContext = scope.ServiceProvider.GetService<IWorkflowNodeExecutionContext>()?.Begin(scope.ServiceProvider);

        // Job work runs outside the activation scheduler; Grain.GrainFactory checks that context.
        // The host's injected factory is safe on the background worker thread.
        var run = scope.ServiceProvider.GetRequiredService<IGrainFactory>().GetGrain<IWorkflowRun>(item.Run);
        var execution = NodeExecutionKey.Parse(item.Execution);
        var attempt = new WorkflowAttempt(item.Attempt);
        try
        {
            var performer = scope.ServiceProvider.GetRequiredService<IWorkflowNodePerformers>().For(new CapabilityReference(item.Capability, item.CapabilityVersion));
            await new WorkflowNodePerformance(run, item, execution, attempt, performer).Perform(cancellationToken);
            return JobStepResult.Succeeded();
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return JobStepResult.Failed($"The outcome of '{item.Execution}' could not be reported to workflow run '{item.Run}': {exception.Message}");
        }
    }

    /// <inheritdoc/>
    protected override ValueTask<object?> CreateCancelledResultFromCurrentState(WorkflowNodeStepState currentState) => ValueTask.FromResult<object?>(null);
}
