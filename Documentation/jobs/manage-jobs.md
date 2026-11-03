---
title: Start, stop and delete jobs
description: Start, stop, drain, resume and delete jobs from your application through the IJobs service, and read each outcome without exceptions.
---

You start a job from an endpoint and you stop or delete it from an operator action. Both are calls on a grain that may
live on another silo, possibly one running a different release during a rolling upgrade. This page shows the
supported way to make those calls and how to read what comes back.

## Use `IJobs`, not the generic manager methods

During a rolling upgrade, a shared jobs manager may still run on the older release. Calling its generic
`Start<TJob, TRequest>` or `GetJobsOfType<TJob, TRequest>` sends those types to that silo, which cannot decode a
job or request introduced by the newer release. Use the singleton `IJobs` from `Cratis.Choreography.Jobs` for
starting, stopping, draining and deleting instead. `AddCratisChoreography` registers it.

`IJobs` is a local service, not a grain. It addresses the job grain directly from the caller's release, and
Orleans places that grain on a silo that hosts its type. No generic job or request type passes through the shared
manager. The generic manager methods stay available for single-version clusters and carry documentation warnings
rather than `[Obsolete]` build warnings.

A job is addressed by a `JobKey` of a **scope** and a **namespace**, two strings the engine treats as opaque and
the storage uses to find the right database (see [Storage](../storage/index.md)). A host that serves several
tenants typically uses the event store name as the scope and the tenant namespace as the namespace. This excerpt
belongs in a host with Orleans and jobs storage configured, and assumes a serializable request and the
`IRebuildTenantIndex : IJob<RebuildIndexRequest>` grain from [Planning a job](planning.md):

```csharp
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
}
```

The excerpt uses `Cratis.Monads` for `Result` and `Cratis.Choreography.Jobs` for everything else.

## Read the outcome of a start

`Start` returns the job identity only when the job accepts the request. Success means *started*, not completed.
`Start(key, jobId, request)` lets you supply the identity, and a duplicate start of that identity keeps the job's
usual refusal behavior instead of starting a second plan. On refusal, inspect the `StartJobError`:

| Error | Meaning |
| --- | --- |
| `AlreadyBeenPrepared`, `AlreadyCompleted` | The job already exists and was started or has finished |
| `NotAdmitted` | The job's owner refused the start; nothing was written (see [Admission](admission-and-external-input.md)) |
| `InvalidStepPlan`, `CouldNotPrepareJobSteps` | The plan could not be saved or a step refused preparation (see [Planning](planning.md)) |
| `NoJobStepsToStart` | The plan was empty and the job completed |
| `FailedStartingSomeJobSteps` | The job **is running**; steps that failed to start count as failed |
| `AllJobStepsFailedStarting` | No step started and the job is not running |
| `Unknown` | An unexpected failure or a write that was not durable |

Starting also remembers the scope and namespace for recovery without making `Start` wait; see
[Recovery](recovery.md) for the complete catalog you should register.

## Read the outcome of a stop or delete

Stop and delete read only the stored job type, so an unreadable request from a newer release does not hide which
release owns the job. Match their nested result records:

| Outcome | Stop | Delete |
| --- | --- | --- |
| Accepted by the job | `StopJobResult.Stopped(JobId)` | `DeleteJobResult.Deleted(JobId)` |
| No stored job | `StopJobResult.NotFound(JobId)` | `DeleteJobResult.NotFound(JobId)` |
| Type unavailable in this release | `StopJobResult.UnknownJobType(JobId, JobType)` | `DeleteJobResult.UnknownJobType(JobId, JobType)` |
| Refused by the job | `StopJobResult.Refused(JobId, StopJobError)` | `DeleteJobResult.Refused(JobId, RemoveJobError)` |

An unknown type can belong to a newer release or to a removed job. Neither operation resolves a grain or changes
state in that case: route the operation to a release that knows the type. Storage and grain-call exceptions
propagate instead of being reported as stopped or deleted. These result records stay local and are not serialized
across grains.

A stop refuses with `StopJobError.JobIsCompleted` for a finished job, `JobIsNotRunning` for one that is not running
and `CouldNotPersistStop` when the stop could not be written, which leaves the job as it was. Delete calls the
job's `Remove`, which stops its steps first. It refuses a completed job with `RemoveJobError.JobIsCompleted`; kept
terminal records go away through the retention cleanup. When the removal cannot be persisted it refuses with
`CouldNotPersistRemoval` and can be retried. Acceptance does not promise that every step has finished its own
stop or removal callback. Third-party storage providers should override `IJobStorage.GetJobType` with a
metadata-only read, because the compatibility fallback reads the full job and can still fail on an unknown request.

## Stop a job and wait until it is drained

`Stop` is accepted as soon as the job has persisted it, and its steps finish stopping afterwards. When you need to
know that nothing of the job runs any more, before you read its steps' checkpoints, hand its work to someone else
or delete it, use `StopAndDrain`. This excerpt adds it to `TenantJobs`:

```csharp
public async Task<bool> IsDrained(string eventStoreName, string tenantNamespace, JobId jobId) =>
    await jobs.StopAndDrain(new JobKey(eventStoreName, tenantNamespace), jobId, TimeSpan.Zero) is
        DrainJobResult.Drained or DrainJobResult.Completed;
```

| Outcome | Meaning |
| --- | --- |
| `DrainJobResult.Drained(JobId)` | The job is stopped and drained |
| `DrainJobResult.TimedOut(JobId)` | The job is stopped, but not every step drained in time; call again and the next call asks only the steps that have not drained |
| `DrainJobResult.Completed(JobId)` | The job had already completed or failed and none of its step work runs |
| `DrainJobResult.NotFound(JobId)` | No stored job |
| `DrainJobResult.UnknownJobType(JobId, JobType)` | The stored type is unavailable in this release |

`IJobDrain.StopAndDrain` on the job grain is the same operation, and every job implements it. The timeout is capped
at half the Orleans response timeout less a margin, because the steps' own reports queue behind the drain.
`TimeSpan.Zero` polls without waiting, and polling never cuts a step short: a step refuses its callbacks only once
its own `StepStopTimeout` has passed since its stop began. A drained job is a stopped job, so `Resume()` runs it
again and automatic recovery leaves it alone. Asking again after `Drained` writes nothing and runs no hook. What
`Drained` guarantees, and what it does not, is in [Stopping and draining](stopping-and-draining.md).

:::caution[Do not wait for a drain from a grain your job calls back into]
A drain with a timeout above zero holds the job's turn while it waits, and your call holds yours. If step work or
the job's hooks call back into your grain without interleaving, including step work that calls its own job,
neither can proceed: the drain times out or your call stalls until the response timeout. From such a grain, poll
with `TimeSpan.Zero`, for instance from a grain timer, or make those callbacks interleave. Keep hooks and
`CanResume` to `[AlwaysInterleave]` or read-only calls, or storage.
:::

:::note[Every silo must be able to drain]
A step hosted by a silo that predates drain cannot be drained, so its job answers `TimedOut` until that silo is
upgraded. A write by such a silo can also drop the pending drain; the job is still stopped and nothing of it
runs, and your next `StopAndDrain` drains it again. Use `StopAndDrain` once every silo runs Cratis.Orleans 1.20.0
or later, or any Cratis.Choreography release.
:::

## Resume and retry through the manager

A job resumes on its own after a silo restart; see [Recovery](recovery.md). To resume one yourself, use the manager
for the scope and namespace. `IJobsManager.Resume(jobId)` returns `true` when the job was taken forward (resumed,
already running or finalized as completed) and `false` when nothing took it. Treat `false` as a refusal, because a
job nobody took stays exactly as it was.

`ResumeIfRunning(jobId)` resumes only a job whose stored status is `Running`. The check runs in the job grain's own
turn, so a `Stop` or `Remove` is serialized before or after the whole call and can never land between the check and
the resume. For a `Running` job it behaves exactly like `Resume()`. For any other status it changes nothing, runs
no hook, starts no step and returns `ResumeJobSuccess.JobIsNotRunning`. A manager that cannot reach the job answers
`CannotResumeJobError.Unknown`. `Resume` stays unrestricted, so an operator can still resume a stopped job.

```csharp
var manager = grainFactory.GetJobsManager("store", "tenant");
var result = await manager.ResumeIfRunning(jobId);
result.Match(
    success => success == ResumeJobSuccess.JobIsNotRunning ? Skipped(jobId) : Resumed(jobId),
    error => Failed(jobId, error));
```

`Skipped`, `Resumed` and `Failed` stand in for your own handling. To run the failed steps of a finished job again,
call `RetryFailedSteps(jobId)`, which a job type must opt into (see
[Stages, failures and retries](stages-failures-and-retries.md#run-the-failed-steps-again)).
