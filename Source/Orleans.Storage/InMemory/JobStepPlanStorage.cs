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
    readonly ConcurrentDictionary<JobStepPlanKey, IReadOnlyList<PlannedJobStep>> _plans = new();

    /// <inheritdoc/>
    public Task<Catch> Save(JobId jobId, Guid incarnation, IReadOnlyList<PlannedJobStep> plan)
    {
        _plans[new(jobId, incarnation)] = [.. plan];
        return Task.FromResult(Catch.Success());
    }

    /// <inheritdoc/>
    public Task<Catch<IReadOnlyList<PlannedJobStep>>> GetForJob(JobId jobId, Guid incarnation) =>
        Task.FromResult(Catch<IReadOnlyList<PlannedJobStep>>.Success(_plans.TryGetValue(new(jobId, incarnation), out var plan) ? plan : []));

    /// <inheritdoc/>
    public Task<Catch<IReadOnlyList<JobStepPlanKey>>> GetJobsWithPlans() =>
        Task.FromResult(Catch<IReadOnlyList<JobStepPlanKey>>.Success([.. _plans.Keys]));

    /// <inheritdoc/>
    public Task<Catch> RemoveForJob(JobId jobId, Guid incarnation)
    {
        _plans.TryRemove(new(jobId, incarnation), out _);
        return Task.FromResult(Catch.Success());
    }
}
