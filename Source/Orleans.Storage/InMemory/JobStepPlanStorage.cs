// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Orleans.Storage.InMemory;

/// <summary>
/// Represents an in-memory implementation of <see cref="IJobStepPlanStorage"/>.
/// </summary>
public sealed class JobStepPlanStorage : IJobStepPlanStorage
{
    readonly ConcurrentDictionary<JobId, IReadOnlyList<PlannedJobStep>> _plans = new();

    /// <inheritdoc/>
    public Task<Catch> Save(JobId jobId, IReadOnlyList<PlannedJobStep> plan)
    {
        _plans[jobId] = [.. plan];
        return Task.FromResult(Catch.Success());
    }

    /// <inheritdoc/>
    public Task<Catch<IReadOnlyList<PlannedJobStep>>> GetForJob(JobId jobId) =>
        Task.FromResult(Catch<IReadOnlyList<PlannedJobStep>>.Success(_plans.TryGetValue(jobId, out var plan) ? plan : []));

    /// <inheritdoc/>
    public Task<Catch<IReadOnlyList<JobId>>> GetJobsWithPlans() =>
        Task.FromResult(Catch<IReadOnlyList<JobId>>.Success([.. _plans.Keys]));

    /// <inheritdoc/>
    public Task<Catch> RemoveForJob(JobId jobId)
    {
        _plans.TryRemove(jobId, out _);
        return Task.FromResult(Catch.Success());
    }
}
