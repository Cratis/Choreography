// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Jobs.Stages;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Scopes;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Choreography.Workflows.Execution;

/// <summary>
/// Runs one wave of a workflow run - the node executions that were ready together - as throttled,
/// durable job steps.
/// </summary>
/// <param name="scopeFactory">The scope the performers are resolved from, in the scope the job belongs to.</param>
/// <param name="scopes">The <see cref="IWorkflowExecutionScopes"/> that establish the context work for a scope runs in.</param>
/// <remarks>
/// Every step is prepared up front: one per item, or, for a staged performer, one per operation in a
/// stage of its own, so each operation is behind a barrier and is recorded before the next starts. A
/// failing stage never stops the next one - the next operation finds that its predecessor recorded
/// nothing and does nothing itself. Steps start in the background, so the run grain starting the wave
/// never waits for them. Nothing is kept after the job ends: the run's history lives in its journal.
/// </remarks>
[JobType("WorkflowWaveJob")]
[GrainType("cratis-orleans-workflow-wave-job")]
public class WorkflowWaveJob(IServiceScopeFactory scopeFactory, IWorkflowExecutionScopes scopes) : Job<WorkflowWaveRequest, JobState>, IWorkflowWaveJob
{
    /// <summary>
    /// The distance between the stages of two consecutive operations of a staged performer.
    /// </summary>
    public const int StageSpacing = 10;

    /// <inheritdoc/>
    protected override bool KeepAfterCompleted => false;

    /// <inheritdoc/>
    protected override bool KeepAfterCompletedWithFailures => false;

    /// <inheritdoc/>
    protected override bool StartStepsInBackground => true;

    /// <inheritdoc/>
    protected override async Task<IImmutableList<JobStepDetails>> PrepareSteps(WorkflowWaveRequest request)
    {
        using var scopeContext = scopes.Enter(scopes.ScopeOf(new JobsManagerKey(JobKey.Scope, JobKey.Namespace)));
        await using var scope = scopeFactory.CreateAsyncScope();
        var performers = scope.ServiceProvider.GetRequiredService<IWorkflowNodePerformers>();

        return request.Items.SelectMany(item => StepsFor(performers, item)).ToImmutableList();
    }

    /// <inheritdoc/>
    protected override JobStageFailureBehavior GetFailureBehaviorFor(JobStepStage stage) => JobStageFailureBehavior.ContinueWithNextStage;

    /// <inheritdoc/>
    protected override JobDetails GetJobDetails() => $"Run wave {Request.Wave} of workflow run {Request.Run}";

    IEnumerable<JobStepDetails> StepsFor(IWorkflowNodePerformers performers, WorkflowWaveItem item)
    {
        if (performers.For(new CapabilityReference(item.Capability, item.CapabilityVersion)) is IStagedWorkflowNodePerformer staged)
        {
            return staged.Operations.Select((operation, index) => CreateStep<IWorkflowNodeStep>(item with { Operation = operation.Value }, index * StageSpacing));
        }

        return [CreateStep<IWorkflowNodeStep>(item, 0)];
    }
}
