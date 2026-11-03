---
title: Run jobs while two releases share a cluster
description: Start, stop, delete and drain jobs safely during a rolling upgrade, and know which job features wait until every silo runs the new release.
---

A rolling upgrade means silos of two releases serve one cluster for a while. Orleans identifies a grain method on the wire by its interface, name and parameter types, so a silo can receive a call it cannot decode. For jobs this bites in one specific place: the shared jobs manager. If it still runs on the older release, a call that carries a job or request type introduced by the newer release arrives at a silo that has never heard of it.

This page shows the calls that stay safe in that window, the job features that wait until every silo runs the new release, and what to do before a rollback. The interface rules that make mixed clusters possible at all are in [Upgrading](index.md).

## Use IJobs instead of the jobs manager

The generic `IJobsManager.Start<TJob, TRequest>` and `GetJobsOfType<TJob, TRequest>` send the job and request types to the manager grain, wherever it is activated. Use the singleton `IJobs` from `Cratis.Choreography.Jobs` for starting, stopping, deleting and draining instead. `AddCratisChoreography` registers it with the host.

`IJobs` is a local service, not a grain. It addresses the job grain directly from the caller's release, and Orleans places that grain on a silo that hosts its type. No generic job or request type passes through the shared manager. The generic manager methods remain available for clusters on one version; they carry documentation warnings, not `[Obsolete]` build warnings.

A job is addressed by a `JobKey`: the storage scope (for a multi-tenant host, the event store name) and the namespace (the tenant). This excerpt belongs in an existing host with Orleans and jobs storage configured. It assumes your serializable `RebuildIndexRequest : IJobRequest` and your `IRebuildTenantIndex : IJob<RebuildIndexRequest>` grain:

```csharp
using Cratis.Monads;
using Cratis.Choreography.Jobs;

public class TenantJobs(IJobs jobs)
{
    public Task<Result<JobId, StartJobError>> Start(
        string eventStoreName, string tenantNamespace, RebuildIndexRequest request) =>
        jobs.Start<IRebuildTenantIndex, RebuildIndexRequest>(
            new JobKey(eventStoreName, tenantNamespace), request);

    public Task<StopJobResult> Stop(string eventStoreName, string tenantNamespace, JobId jobId) =>
        jobs.Stop(new JobKey(eventStoreName, tenantNamespace), jobId);

    public Task<DeleteJobResult> Delete(string eventStoreName, string tenantNamespace, JobId jobId) =>
        jobs.Delete(new JobKey(eventStoreName, tenantNamespace), jobId);

    public async Task<bool> IsDrained(string eventStoreName, string tenantNamespace, JobId jobId) =>
        await jobs.StopAndDrain(new JobKey(eventStoreName, tenantNamespace), jobId, TimeSpan.Zero) is
            DrainJobResult.Drained or DrainJobResult.Completed;
}
```

`Start` returns the job identity only when the job accepts the request, so inspect `StartJobError` on refusal. Success means started, not completed. If you supply an identity with `Start(key, jobId, request)`, a duplicate start keeps the job's existing refusal behavior rather than starting another plan. `FailedStartingSomeJobSteps` means the job keeps running until its remaining steps finish, with the steps that failed to start counted as failed; it does not mean the job did not start. `AllJobStepsFailedStarting` means no step started and the job is not running.

## Match the stop and delete results

Stop and delete read only the stored job type, so a request from a newer release that this silo cannot deserialize does not hide which release owns the job. Match the nested result records:

| Outcome | Stop | Delete |
| --- | --- | --- |
| Accepted by the job | `StopJobResult.Stopped(JobId)` | `DeleteJobResult.Deleted(JobId)` |
| No stored job | `StopJobResult.NotFound(JobId)` | `DeleteJobResult.NotFound(JobId)` |
| Type unavailable in this release | `StopJobResult.UnknownJobType(JobId, JobType)` | `DeleteJobResult.UnknownJobType(JobId, JobType)` |
| Refused by the job | `StopJobResult.Refused(JobId, StopJobError)` | `DeleteJobResult.Refused(JobId, RemoveJobError)` |

An unknown type can belong to a newer release or to a job class that was removed. Neither operation resolves a grain or changes state in that case, so route the operation to a release that knows the type. Storage exceptions and grain-call exceptions propagate; they are never reported as stopped or deleted.

Delete keeps the manager's removal rules: it calls the job's `Remove`, which stops its steps, and a completed job can refuse. If the removal cannot be persisted it is refused with `RemoveJobError.CouldNotPersistRemoval` and can be retried. Acceptance does not promise that every step has already finished its stop or removal callback; use `StopAndDrain` when that matters (see [Stopping and draining](../jobs/stopping-and-draining.md)). These result records stay local and are never serialized across grains.

Third-party storage providers should override `IJobStorage.GetJobType` with a metadata-only read. Its compatibility fallback reads the whole job, and that read can still fail on an unknown request.

## Recovery keeps working across the boundary

Starting a job through `IJobs` remembers its scope and namespace for recovery and touches the shared manager once per locally seen key with an existing non-generic query, which keeps the manager's normal cleanup activation. Start does not wait for that query, and a failed touch is logged without failing the job start. The job persists its preparation before answering, independently of the manager.

Remembered keys are not a durable catalog. Register singleton `IJobsManagerKeys` providers on every silo for complete recovery, as described in [Recover jobs after a silo is lost](../jobs/recovery.md#tell-recovery-which-namespaces-exist). During release overlap, an older manager skips job types it does not know. Those jobs can resume once their manager runs on a release that knows them and another recovery pass occurs. `IJobs` does not change that recovery boundary.

## Large step plans while releases are mixed

Every job class advertises `JobStepPlanStorageSupportAttribute` in the cluster manifest. A job keeps a step plan outside the job document only when every silo that hosts the job class advertises that support and the storage provider can hold the plan. Otherwise the plan stays inline whatever its size, and a plan too large for the job document fails the job cleanly with a recorded reason. Nothing needs configuring: once the last older silo leaves, new jobs externalize large plans, while a job started earlier keeps its inline plan. [Planning](../jobs/planning.md) explains how oversized plans are stored and `JobsOptions.MaxInlineStepPlanSize` sets the threshold.

Before you roll back, drain jobs that use externalized preparation and let them finish. Older silos do not understand the plan reference and may drop it when they rewrite the job, so they cannot resume that preparation. The plan incarnation is additive, and mixed releases must preserve it. Drain the same jobs before upgrading from, or rolling back to, an earlier external-plan format that had no incarnation: plans without one are not loaded or reaped by this release, and you inspect and repair those jobs through your application's recovery procedure.

## Features that wait for every silo

Each of these is safe to deploy while releases are mixed, but takes full effect only when every silo that hosts the job runs a release that includes it. Releases before 2.0.0 were published as Cratis.Orleans, and the numbers in the table follow that numbering.

| Feature | What a silo on an older release does | Requirement |
| --- | --- | --- |
| `StopAndDrain` | A step hosted there cannot be drained, so its job answers `TimedOut`. A write there can also drop a pending drain; the job is still stopped, and your next `StopAndDrain` drains it again. | Cratis.Orleans 1.20.0 or later |
| Execution generations | A step or job there sends and reports no generation and keeps the earlier stop-versus-resume behavior. | 1.20.0 or later |
| `IJobAdmission` | It does not ask at all. | 1.21.0 or later |
| Finalized stops | It writes a stop without the finalized flag, so `OnStopped` runs once more on the first rehydration afterwards. | 1.26.0 or later |
| Job and step placement | Placement is a cluster-wide agreement, so a silo without it cannot honor the strategy. Enable it only once every silo has it. | 1.30.0 or later |

## Next steps

- [Upgrading](index.md) explains the grain interface rules behind all of this.
- [Manage jobs](../jobs/manage-jobs.md) covers the same `IJobs` calls outside an upgrade.
