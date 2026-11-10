// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using JobStepError = Cratis.Choreography.Storage.Jobs.JobStepError;

namespace Cratis.Choreography.Storage.Sql.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStepStorage"/> using SQL.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobStepStorage"/> class.
/// </remarks>
/// <param name="contextFactory">Factory creating the <see cref="JobsDbContext"/> for the scope and namespace being stored for.</param>
public class JobStepStorage(Func<JobsDbContext> contextFactory) : IJobStepStorage
{
    /// <inheritdoc/>
    public Task<Catch> RemoveAllForJob(JobId jobId) => RemoveWhere(js => js.JobId == jobId.Value);

    /// <inheritdoc/>
    public Task<Catch> RemoveAllNonFailedForJob(JobId jobId) => RemoveWhere(js => js.JobId == jobId.Value && js.Status != JobStepStatus.Failed);

    /// <inheritdoc/>
    public async Task<Catch> Remove(JobId jobId, JobStepId jobStepId)
    {
        try
        {
            await using var dbContext = contextFactory();
            var jobStep = await dbContext.JobSteps.FirstOrDefaultAsync(js => js.JobId == jobId.Value && js.JobStepId == jobStepId.Value);
            if (jobStep is not null)
            {
                dbContext.JobSteps.Remove(jobStep);
                await dbContext.SaveChangesAsync();
            }
            return Catch.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<IImmutableList<JobStepState>>> GetForJob(JobId jobId, params JobStepStatus[] statuses)
    {
        try
        {
            await using var dbContext = contextFactory();
            var jobSteps = await StepsOfJob(dbContext, jobId, statuses).ToListAsync();
            return jobSteps.Select(js => js.ToJobStepState()).ToImmutableList();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<int>> CountForJob(JobId jobId, params JobStepStatus[] statuses)
    {
        try
        {
            await using var dbContext = contextFactory();
            return await StepsOfJob(dbContext, jobId, statuses).CountAsync();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<IReadOnlyDictionary<JobStepStatus, int>>> CountByStatus(JobId jobId)
    {
        try
        {
            await using var dbContext = contextFactory();
            var groups = await dbContext.JobSteps
                .Where(js => js.JobId == jobId.Value)
                .GroupBy(js => js.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync();
            IReadOnlyDictionary<JobStepStatus, int> counts = groups.ToDictionary(_ => _.Status, _ => _.Count);
            return Catch<IReadOnlyDictionary<JobStepStatus, int>>.Success(counts);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public Catch<IObservable<IEnumerable<JobStepState>>> ObserveForJob(JobId jobId)
    {
        try
        {
            // A simple observable seeded with the current data; this could be enhanced with actual database change
            // tracking in the future.
            var subject = new BehaviorSubject<IEnumerable<JobStepState>>([]);

            Task.Run(async () =>
            {
                var jobSteps = await GetForJob(jobId);
                if (jobSteps.TryPickT0(out var jobStepList, out _))
                {
                    subject.OnNext(jobStepList);
                }
            });

            return subject;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<TJobStepState, JobStepError>> Read<TJobStepState>(JobId jobId, JobStepId jobStepId)
    {
        try
        {
            if (JobStepStateType.Verify(typeof(TJobStepState)).TryGetError(out var error))
            {
                return error;
            }

            await using var dbContext = contextFactory();
            var jobStep = await dbContext.JobSteps.FirstOrDefaultAsync(js => js.JobId == jobId.Value && js.JobStepId == jobStepId.Value);

            if (jobStep is null)
            {
                return JobStepError.NotFound;
            }

            if (string.IsNullOrEmpty(jobStep.StateJson))
            {
                return JobStepError.NotFound;
            }

            var jobStepState = JsonSerializer.Deserialize<TJobStepState>(jobStep.StateJson);
            return jobStepState is not null ? jobStepState : JobStepError.NotFound;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<None, JobStepError>> Save<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState state)
    {
        try
        {
            if (JobStepStateType.Verify(typeof(TJobStepState)).TryGetError(out var error))
            {
                return error;
            }

            await using var dbContext = contextFactory();
            var existing = await dbContext.JobSteps.FirstOrDefaultAsync(js => js.JobId == jobId.Value && js.JobStepId == jobStepId.Value);
            var entity = state is JobStepState jobStepState ?
                jobStepState.ToEntity(existing?.Id ?? Guid.NewGuid()) :
                new JobStep
                {
                    Id = existing?.Id ?? Guid.NewGuid(),
                    JobId = jobId.Value,
                    JobStepId = jobStepId.Value,
                    StateJson = JsonSerializer.Serialize(state)
                };

            if (existing is null)
            {
                dbContext.JobSteps.Add(entity);
            }
            else
            {
                dbContext.JobSteps.Entry(existing).CurrentValues.SetValues(entity);
            }

            await dbContext.SaveChangesAsync();
            return default(None);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<None, JobStepError>> MoveToFailed<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState jobStepState)
    {
        try
        {
            if (JobStepStateType.Verify(typeof(TJobStepState)).TryGetError(out var error))
            {
                return error;
            }

            await using var dbContext = contextFactory();
            var jobStep = await dbContext.JobSteps.FirstOrDefaultAsync(js => js.JobId == jobId.Value && js.JobStepId == jobStepId.Value);

            if (jobStep is not null)
            {
                // Update status to failed and save the state
                jobStep.Status = JobStepStatus.Failed;
                jobStep.StateJson = JsonSerializer.Serialize(jobStepState);

                if (jobStepState is JobStepState stepState)
                {
                    jobStep.Type = stepState.Type.Value;
                    jobStep.Name = stepState.Name.Value;
                    jobStep.IsPrepared = stepState.IsPrepared;
                }

                await dbContext.SaveChangesAsync();
            }

            return default(None);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    static IQueryable<JobStep> StepsOfJob(JobsDbContext dbContext, JobId jobId, JobStepStatus[] statuses)
    {
        var query = dbContext.JobSteps.Where(js => js.JobId == jobId.Value);
        return statuses.Length > 0 ? query.Where(js => statuses.Contains(js.Status)) : query;
    }

    async Task<Catch> RemoveWhere(Expression<Func<JobStep, bool>> predicate)
    {
        try
        {
            await using var dbContext = contextFactory();
            dbContext.JobSteps.RemoveRange(dbContext.JobSteps.Where(predicate));
            await dbContext.SaveChangesAsync();
            return Catch.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
