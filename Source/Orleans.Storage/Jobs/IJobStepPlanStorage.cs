// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Storage.Jobs;

/// <summary>
/// Defines the storage for the step plans that are too large to live inside the job document.
/// </summary>
/// <remarks>
/// A plan is kept here only between its save, which happens before the job durably writes
/// <see cref="JobStatus.PreparingSteps"/>, and the job durably reaching running or a terminal state. It is never
/// removed before that, so a job that recovers always finds its plan.
/// </remarks>
public interface IJobStepPlanStorage
{
    /// <summary>
    /// Saves one incarnation of a job's plan, replacing a previous attempt of that incarnation.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> the plan belongs to.</param>
    /// <param name="incarnation">A fresh identity persisted in job state before this save; retained across storage retries.</param>
    /// <param name="plan">The planned steps, in order.</param>
    /// <returns>Awaitable task.</returns>
    Task<Catch> Save(JobId jobId, Guid incarnation, IReadOnlyList<PlannedJobStep> plan);

    /// <summary>
    /// Gets the plan of a job.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> to get the plan for.</param>
    /// <param name="incarnation">The incarnation from the job's own state.</param>
    /// <returns>The planned steps in the order they were saved; empty when the job has no saved plan.</returns>
    Task<Catch<IReadOnlyList<PlannedJobStep>>> GetForJob(JobId jobId, Guid incarnation);

    /// <summary>
    /// Gets the job and incarnation of each saved plan, from the plan documents themselves.
    /// </summary>
    /// <returns>The identities of the saved plans.</returns>
    Task<Catch<IReadOnlyList<JobStepPlanKey>>> GetJobsWithPlans();

    /// <summary>
    /// Removes the plan of a job.
    /// </summary>
    /// <param name="jobId"><see cref="JobId"/> to remove the plan for.</param>
    /// <param name="incarnation">The incarnation from the job state or orphan plan documents.</param>
    /// <returns>Awaitable task.</returns>
    Task<Catch> RemoveForJob(JobId jobId, Guid incarnation);
}
