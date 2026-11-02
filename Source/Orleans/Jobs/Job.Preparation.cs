// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Monads;
using Cratis.Orleans.Jobs.Stages;
using Cratis.Orleans.Serialization;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Orleans.Jobs;

public abstract partial class Job<TRequest, TJobState>
{
    bool _dispatchReady;
    bool _hasDispatchedSteps;

    JsonSerializerOptions StepPlanSerializerOptions =>
        ServiceProvider.GetService<JsonSerializerOptions>() ?? CratisJsonSerializer.CreateSerializerOptions();

    bool HasStepPlan => State.StepPlan is not null || State.HasStoredStepPlan;

    List<PlannedJobStep> SaveStepPlan(IImmutableList<JobStepDetails> steps) => steps.Select(SavePlannedStep).ToList();

    PlannedJobStep SavePlannedStep(JobStepDetails step)
    {
        try
        {
            var type = step.Request.GetType();
            var json = JsonSerializer.Serialize(step.Request, type, StepPlanSerializerOptions);
            var restored = JsonSerializer.Deserialize(json, type, StepPlanSerializerOptions);
            var restoredJson = JsonSerializer.Serialize(restored, type, StepPlanSerializerOptions);
            if (restored is null || !JsonNode.DeepEquals(JsonNode.Parse(json), JsonNode.Parse(restoredJson)))
            {
                throw new JobStepRequestCannotRoundTrip(step.Id);
            }
            return new(step.Id, step.Type, step.Stage, type.AssemblyQualifiedName!, json);
        }
        catch (Exception exception) when (exception is not JobStepRequestCannotRoundTrip)
        {
            throw new JobStepRequestCannotRoundTrip(step.Id, exception);
        }
    }

    /// <summary>
    /// Gets the step plan, from the job document or from the plan store.
    /// </summary>
    /// <returns>The plan, or null when the job has none.</returns>
    /// <exception cref="InvalidOperationException">The job has a stored plan but its storage has no plan store.</exception>
    async Task<IReadOnlyList<PlannedJobStep>?> LoadStepPlan()
    {
        if (State.StepPlan is { } plan)
        {
            return plan;
        }
        if (!State.HasStoredStepPlan)
        {
            return null;
        }

        var stored = await (Storage.StepPlans ?? throw new InvalidOperationException("The job has a stored step plan, but its storage has no step plan store")).GetForJob(JobId);
        stored.RethrowError();

        // A stored plan is never empty: an empty answer means it is gone, and a job must not run or finish on that.
        return stored.AsT0.Count == 0 ? null : stored.AsT0;
    }

    /// <summary>
    /// Removes the stored step plan, which is only safe once the job is durably running or terminal.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    async Task RemoveStoredStepPlan()
    {
        if (Storage.StepPlans is not { } store)
        {
            return;
        }

        var removed = await store.RemoveForJob(JobId);
        if (removed.TryGetException(out var exception))
        {
            _logger.FailedRemovingStoredStepPlan(exception);
        }
    }

    /// <summary>
    /// Writes Running and drops the plan reference. A failed or unacknowledged write retires the activation.
    /// </summary>
    /// <returns>True when Running is durable.</returns>
    /// <remarks>
    /// The write may have committed, and then cleanup may delete the plan. The memory is restored, but the activation persists
    /// nothing more and the next one recovers from what is durable.
    /// </remarks>
    async Task<bool> TryEnterRunning()
    {
        var plan = State.StepPlan;
        var stored = State.HasStoredStepPlan;
        State.StepPlan = null;
        State.HasStoredStepPlan = false;
        if (await TryWriteStatusChanged(JobStatus.Running))
        {
            if (stored)
            {
                await RemoveStoredStepPlan();
            }
            return true;
        }
        State.StepPlan = plan;
        State.HasStoredStepPlan = stored;
        RetireActivation();
        return false;
    }

    /// <summary>
    /// Puts the plan where it belongs: outside the job document when it is large, storage can keep it and every silo supports it.
    /// </summary>
    /// <param name="plan">The plan.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The plan documents are written before the job document says it has them, so a crash in between leaves
    /// documents that no job refers to, never a job that refers to missing ones.
    /// </remarks>
    async Task PlaceStepPlan(List<PlannedJobStep> plan)
    {
        // An empty plan stays inline: a stored plan is never empty, so an empty answer from the store means it is missing.
        var size = plan.Count == 0 ? 0 : JsonSerializer.SerializeToUtf8Bytes(plan, StepPlanSerializerOptions).Length;
        if (plan.Count == 0 || size <= JobsOptionsOrDefault.MaxInlineStepPlanSize || Storage.StepPlans is not { } store)
        {
            State.StepPlan = plan;
            State.HasStoredStepPlan = false;
            return;
        }

        if (!JobStepPlanStorageIsSupportedByEverySilo())
        {
            _logger.KeepingLargeStepPlanInline(size);
            State.StepPlan = plan;
            State.HasStoredStepPlan = false;
            return;
        }

        var saved = await store.Save(JobId, plan);
        saved.RethrowError();
        State.StepPlan = null;
        State.HasStoredStepPlan = true;
    }

    bool JobStepPlanStorageIsSupportedByEverySilo()
    {
        var support = ServiceProvider.GetService<IJobStepPlanStorageSupport>()
            ?? (ServiceProvider.GetService<IClusterManifestProvider>() is { } manifestProvider ? new ClusterManifestJobStepPlanStorageSupport(manifestProvider) : null);
        return support?.IsSupportedByEverySilo(this.GetGrainId().Type) ?? false;
    }

    async Task RunDispatchHook(bool initializationMayBeIncomplete)
    {
        _dispatchReady = false;
        await OnBeforeDispatchingJobSteps(new(initializationMayBeIncomplete));
        _dispatchReady = true;
    }

    Task EnsureDispatchHook() => _dispatchReady
        ? Task.CompletedTask
        : RunDispatchHook(State.Status is JobStatus.PreparingSteps or JobStatus.StartingSteps);

    async Task<Result<ResumeJobSuccess, ResumeJobError>> RecoverStepPreparation()
    {
        if (await LoadStepPlan() is not { } plan)
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.MissingStepPlan);
        }

        var stored = await Storage.JobSteps.GetForJob(JobId);
        stored.RethrowError();
        var prepared = stored.AsT0.Where(step => step.IsPrepared).Select(step => step.Id.JobStepId).ToHashSet();
        var pending = plan.Where(step => !prepared.Contains(step.Id)).ToImmutableList();
        var steps = pending.Select(step => new JobStepDetails(
            step.Type,
            step.Id,
            new(JobId, JobKey.Scope, JobKey.Namespace),
            JsonSerializer.Deserialize(step.RequestJson, Type.GetType(step.RequestType, throwOnError: true)!, StepPlanSerializerOptions)!,
            typeof(object)) { Stage = step.Stage }).ToImmutableList();

        // No step can have executed in PreparingSteps. Missing state here is an unprepared step, not a success.
        State.Progress.TotalSteps = plan.Count;
        State.Progress.Stages = JobStages.Plan(plan.Select(step => step.Stage));
        foreach (var step in plan)
        {
            _jobStepStages[step.Id] = step.Stage;
        }
        if (!await TryWriteStatusChanged(JobStatus.PreparingSteps))
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }
        var preparation = await TryPrepareAllJobSteps(CreateGrainsFromJobSteps(steps));
        if (!preparation.TryGetResult(out var allPrepared) || !allPrepared)
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.JobCannotBeResumed);
        }
        if (!await TryWriteStatusChanged(JobStatus.StartingSteps))
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }
        return await RecoverStepStartup();
    }

    async Task<Result<ResumeJobSuccess, ResumeJobError>> RecoverStepStartup()
    {
        if (await LoadStepPlan() is not { } plan)
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.MissingStepPlan);
        }

        // StartingSteps is durable only after every step was prepared. Successful steps may already have deleted
        // their state during a partial dispatch; never recreate them from the plan.
        var stored = await Storage.JobSteps.GetForJob(JobId);
        stored.RethrowError();
        var steps = stored.AsT0;
        var byId = steps.ToDictionary(step => step.Id.JobStepId);
        State.Progress.TotalSteps = plan.Count;
        State.Progress.SuccessfulSteps = plan.Count(step => !byId.TryGetValue(step.Id, out var state) || state.Status is JobStepStatus.CompletedSuccessfully);
        State.Progress.FailedSteps = steps.Count(step => step.Status is JobStepStatus.Failed or JobStepStatus.CompletedWithFailure);
        State.Progress.UnreachableSteps = steps.Count(step => step.Status is JobStepStatus.Unreachable);
        ForgetCountedStops();
        State.Progress.Stages = JobStages.Plan(plan.Select(step => step.Stage));
        foreach (var step in plan)
        {
            _jobStepStages[step.Id] = step.Stage;
            var stage = State.Progress.Stages.FirstOrDefault(progress => progress.Stage == step.Stage);
            if (stage is null)
            {
                continue;
            }
            if (!byId.TryGetValue(step.Id, out var state) || state.Status is JobStepStatus.CompletedSuccessfully)
            {
                stage.SuccessfulSteps++;
            }
            else if (state.Status is JobStepStatus.Failed or JobStepStatus.CompletedWithFailure)
            {
                stage.FailedSteps++;
            }
            else if (state.Status is JobStepStatus.Unreachable)
            {
                stage.UnreachableSteps++;
            }
        }
        _jobStepGrains = steps.Where(step => !IsDone(step.Status)).ToDictionary(step => step.Id.JobStepId, GetJobStepGrain);

        // Reconstructed terminal outcomes are already in the counters. Delayed reports must not count them again.
        _countsFromStorage = true;
        foreach (var step in plan.Where(step => !_jobStepGrains.ContainsKey(step.Id)))
        {
            OutcomeCounted(step.Id);
        }
        await RunResumeHook();
        await RunDispatchHook(initializationMayBeIncomplete: true);
        if (State.Progress.IsCompleted || await PrepareStagesForResume())
        {
            return await FinalizeInsteadOfResuming();
        }

        var resumed = await ResumeCurrentStage(StepsInCurrentStage(), reportAlreadyRunning: false);
        if (State.Status is JobStatus.CompletedSuccessfully or JobStatus.CompletedWithFailures)
        {
            return ResumeJobSuccess.JobIsCompleted;
        }
        if (!await TryEnterRunning())
        {
            return Result.Failed<ResumeJobSuccess, ResumeJobError>(CannotResumeJobError.Unknown);
        }
        return resumed;
    }
}
