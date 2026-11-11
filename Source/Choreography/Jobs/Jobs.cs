// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.ExceptionServices;
using Cratis.Choreography.Storage;
using Cratis.Choreography.Storage.Jobs;
using Cratis.DependencyInjection;
using Cratis.Monads;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Choreography.Jobs;

/// <summary>
/// Represents the version-safe, caller-local entry point for jobs.
/// </summary>
/// <param name="grainFactory">The caller's grain factory.</param>
/// <param name="jobsStorage">Storage resolved by scope and namespace.</param>
/// <param name="jobTypes">The job types known to this release.</param>
/// <param name="logger">The logger.</param>
/// <param name="serviceProvider">The services containing the optional silo-local recovery catalog.</param>
[Singleton]
public class Jobs(
    IGrainFactory grainFactory,
    IJobsStorage jobsStorage,
    IJobTypes jobTypes,
    ILogger<Jobs> logger,
    IServiceProvider serviceProvider) : IJobs
{
    readonly JobsManagerKeys _keys = serviceProvider.GetService<JobsManagerKeys>() ?? new();

    /// <inheritdoc/>
    public Task<Result<JobId, StartJobError>> Start<TJob, TRequest>(JobKey key, TRequest request)
        where TJob : IJob<TRequest>
        where TRequest : class, IJobRequest => Start<TJob, TRequest>(key, JobId.New(), request);

    /// <inheritdoc/>
    public Task<Result<JobId, StartJobError>> Start<TJob, TRequest>(JobKey key, JobId jobId, TRequest request)
        where TJob : IJob<TRequest>
        where TRequest : class, IJobRequest
    {
        var managerKey = new JobsManagerKey(key.Scope, key.Namespace);
        if (_keys.Remember(managerKey))
        {
            _ = TouchManager(managerKey);
        }
        return JobStarter.Start<TJob, TRequest>(grainFactory, key, jobId, request);
    }

    /// <inheritdoc/>
    public async Task<StopJobResult> Stop(JobKey key, JobId jobId)
    {
        return await OnJob<StopJobResult>(
            key,
            jobId,
            async job => (await job.Stop()).Match<StopJobResult>(
                _ => new StopJobResult.Stopped(jobId),
                error => new StopJobResult.Refused(jobId, error)),
            StopMissing);
    }

    /// <inheritdoc/>
    public async Task<DeleteJobResult> Delete(JobKey key, JobId jobId)
    {
        return await OnJob<DeleteJobResult>(
            key,
            jobId,
            async job => (await job.Remove()).Match<DeleteJobResult>(
                _ => new DeleteJobResult.Deleted(jobId),
                error => new DeleteJobResult.Refused(jobId, error)),
            DeleteMissing);
    }

    /// <inheritdoc/>
    public async Task<DrainJobResult> StopAndDrain(JobKey key, JobId jobId, TimeSpan timeout)
    {
        return await OnJob<DrainJobResult>(
            key,
            jobId,
            async job => ToDrainJobResult(jobId, await (job as IJobDrain ?? job.AsReference<IJobDrain>()).StopAndDrain(timeout)),
            DrainMissing);
    }

    static StopJobResult StopMissing(JobId jobId, JobType? type) => type is null
        ? new StopJobResult.NotFound(jobId)
        : new StopJobResult.UnknownJobType(jobId, type);

    static DeleteJobResult DeleteMissing(JobId jobId, JobType? type) => type is null
        ? new DeleteJobResult.NotFound(jobId)
        : new DeleteJobResult.UnknownJobType(jobId, type);

    static DrainJobResult DrainMissing(JobId jobId, JobType? type) => type is null
        ? new DrainJobResult.NotFound(jobId)
        : new DrainJobResult.UnknownJobType(jobId, type);

    static DrainJobResult ToDrainJobResult(JobId jobId, JobDrainStatus status) => status switch
    {
        JobDrainStatus.Drained => new DrainJobResult.Drained(jobId),
        JobDrainStatus.Completed => new DrainJobResult.Completed(jobId),
        _ => new DrainJobResult.TimedOut(jobId)
    };

    async Task<TResult> OnJob<TResult>(JobKey key, JobId jobId, Func<IJob, Task<TResult>> onJob, Func<JobId, JobType?, TResult> missing)
    {
        var lookup = await ResolveJob(key, jobId);
        return await lookup.Match(onJob, type => Task.FromResult(missing(jobId, type)));
    }

    async Task<Result<IJob, JobType?>> ResolveJob(JobKey key, JobId jobId)
    {
        var storedType = await jobsStorage.GetFor(key.Scope, key.Namespace).Jobs.GetJobType(jobId);
        if (storedType.TryGetError(out var error) && error == Storage.Jobs.JobError.NotFound)
        {
            return Result<IJob, JobType?>.Failed(null);
        }
        var type = storedType.Match(
            type => type,
            error => throw new JobTypeReadFailed(jobId, error),
            exception =>
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
                return default;
            });
        return jobTypes.GetClrTypeFor(type).Match(
            clrType => Result<IJob, JobType?>.Success((IJob)grainFactory.GetGrain(clrType, jobId, key)),
            _ => Result<IJob, JobType?>.Failed(type));
    }

    async Task TouchManager(JobsManagerKey key)
    {
        try
        {
            // This existing non-generic call is understood by older managers. Do not rehydrate a job
            // which may still be preparing, and do not make starting it wait for manager activation.
            await grainFactory.GetJobsManager(key.Scope, key.Namespace).GetJobs(new JobQuery { Take = 1 });
        }
        catch (Exception exception)
        {
            logger.ManagerActivationFailed(key, exception);
        }
    }
}
