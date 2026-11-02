// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Monads;
using Cratis.Orleans.Storage.InMemory;
using Cratis.Orleans.Storage.Jobs;

using Catch = Cratis.Monads.Catch;

namespace Cratis.Orleans.Jobs.for_Job.given;

/// <summary>
/// A job whose storage keeps step plans separately and whose every plan counts as too large for the job document.
/// </summary>
public class a_job_with_a_plan_store : a_job_with_a_durable_plan
{
    protected JobStepPlanStorage _planStore = new();
    protected bool _planRemovalFails;
    protected int _planRemovals;
    protected bool _supportedByEverySilo = true;
    protected Result<StartJobError> _start;

    protected override JobsOptions CreateOptions() => new() { MaxConcurrentStepStarts = 1, MaxInlineStepPlanSize = 0 };

    protected async Task<bool> PlanIsStored() => (await _planStore.GetForJob(_jobId)).AsT0.Count == 3;

    protected override IJobStepPlanStorage? CreatePlanStore() => new PlanStoreProbe(this);

    protected override IJobStepPlanStorageSupport? CreatePlanStorageSupport() => new Support(this);

    sealed class Support(a_job_with_a_plan_store owner) : IJobStepPlanStorageSupport
    {
        public bool IsSupportedByEverySilo(GrainType jobGrainType) => owner._supportedByEverySilo;
    }

    sealed class PlanStoreProbe(a_job_with_a_plan_store owner) : IJobStepPlanStorage
    {
        public Task<Catch> Save(JobId jobId, IReadOnlyList<PlannedJobStep> plan) => owner._planStore.Save(jobId, plan);

        public Task<Catch<IReadOnlyList<PlannedJobStep>>> GetForJob(JobId jobId) => owner._planStore.GetForJob(jobId);

        public Task<Catch<IReadOnlyList<JobId>>> GetJobsWithPlans() => owner._planStore.GetJobsWithPlans();

        public Task<Catch> RemoveForJob(JobId jobId)
        {
            owner._planRemovals++;
            return owner._planRemovalFails
                ? Task.FromResult<Catch>(new InvalidOperationException("The plan removal failed"))
                : owner._planStore.RemoveForJob(jobId);
        }
    }
}
