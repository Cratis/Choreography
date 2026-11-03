// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Storage;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OneOf.Types;
using Orleans.Storage;

namespace Cratis.Choreography.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IGrainStorage"/> for handling job state storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobGrainStorageProvider"/> class.
/// </remarks>
/// <param name="jobsStorage"><see cref="IJobsStorage"/> for resolving underlying storage.</param>
public partial class JobGrainStorageProvider(IJobsStorage jobsStorage) : IGrainStorage
{
    readonly ILogger<JobGrainStorageProvider> _logger = NullLogger<JobGrainStorageProvider>.Instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobGrainStorageProvider"/> class with logging.
    /// </summary>
    /// <param name="jobsStorage">The jobs storage.</param>
    /// <param name="logger">The logger.</param>
    public JobGrainStorageProvider(IJobsStorage jobsStorage, ILogger<JobGrainStorageProvider> logger) : this(jobsStorage)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ClearStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var type = typeof(T);
        ThrowIfInvalidJobStateType(type, nameof(ClearStateAsync));

        if (grainId.TryGetGuidKey(out var jobId, out var keyExtension))
        {
            var key = (JobKey)keyExtension!;
            var storage = jobsStorage.GetFor(key.Scope, key.Namespace);
            var jobStorage = storage.Jobs;
            var jobStepStorage = storage.JobSteps;

            await HandleCatch(jobStepStorage.RemoveAllForJob(jobId), type, nameof(ClearStateAsync));
            await HandleCatch(jobStorage.Remove(jobId), type, nameof(ClearStateAsync));

            // Only once the job is gone: until then a job document that still says it has a plan needs it.
            if (grainState.State is JobState { StoredStepPlanIncarnation: { } incarnation } && storage.StepPlans is { } stepPlans)
            {
                try
                {
                    await HandleCatch(stepPlans.RemoveForJob(jobId, incarnation), type, nameof(ClearStateAsync));
                }
                catch (Exception exception)
                {
                    FailedRemovingStoredStepPlan(_logger, exception);
                }
            }
        }
    }

    /// <inheritdoc/>
    public async Task ReadStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var type = typeof(T);
        ThrowIfInvalidJobStateType(type, nameof(ReadStateAsync));

        if (grainId.TryGetGuidKey(out var jobId, out var keyExtension))
        {
            var key = (JobKey)keyExtension!;

            var jobStorage = jobsStorage.GetFor(key.Scope, key.Namespace).Jobs;
            var readStateResult = await jobStorage.Read<T>(jobId);
            await readStateResult.Match(
                state =>
                {
                    grainState.State = state;
                    return Task.CompletedTask;
                },
                _ => Task.CompletedTask,
                error => Task.FromException(new JobGrainStorageProviderError(type, error, nameof(ReadStateAsync))));
        }
    }

    /// <inheritdoc/>
    public async Task WriteStateAsync<T>(string stateName, GrainId grainId, IGrainState<T> grainState)
    {
        var type = typeof(T);
        ThrowIfInvalidJobStateType(type, nameof(WriteStateAsync));

        if (grainId.TryGetGuidKey(out var jobId, out var keyExtension))
        {
            var key = (JobKey)keyExtension!;
            var jobStorage = jobsStorage.GetFor(key.Scope, key.Namespace).Jobs;
            var state = (grainState.State as JobState)!;
            state.Id = jobId;
            await HandleCatchNone(jobStorage.Save(jobId, grainState.State), type, nameof(WriteStateAsync));
        }
    }

    [LoggerMessage(LogLevel.Warning, "Could not remove the stored step plan of the removed job; it is left for the cleanup of orphaned step plans")]
    static partial void FailedRemovingStoredStepPlan(ILogger<JobGrainStorageProvider> logger, Exception exception);

    static void ThrowIfInvalidJobStateType(Type type, string operationName) => JobStateType
        .Verify(type)
        .Switch(_ => { }, error => throw new JobGrainStorageProviderError(type, error, operationName));

    static async Task HandleCatchNone(Task<Catch<None, Storage.Jobs.JobError>> getResult, Type type, string methodName)
    {
        var monad = await getResult;
        await monad.Match(
            _ => Task.CompletedTask,
            error => Task.FromException(new JobGrainStorageProviderError(type, error, methodName)),
            error => Task.FromException(new JobGrainStorageProviderError(type, error, methodName)));
    }

    static async Task HandleCatch(Task<Catch> getResult, Type type, string methodName)
    {
        var monad = await getResult;
        await monad.Match(_ => Task.CompletedTask, error => Task.FromException(new JobGrainStorageProviderError(type, error, methodName)));
    }
}
