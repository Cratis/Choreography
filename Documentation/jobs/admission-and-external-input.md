---
title: Gate a job and deliver outside input to it
description: Refuse a job's execution with IJobAdmission when the rules change, and let outside callers hand data to the job rather than to its steps.
---

Two situations need the owner of a job to reach in from outside. The first is a rule change: a tenant is deactivated, a feature is switched off, and a job that was started under the old rules must not run again, not even after a silo restart resumes it. The second is an external worker that calls your application back with progress while a step is running, and you need to keep what it sent. This page covers both: `IJobAdmission` for the first, and a callback on the job's own grain for the second.

## Admit or refuse a job's execution

An `IJobAdmission` is asked before an execution begins or begins again. A refusal stops the job without running any of its step code:

```csharp
using Cratis.Choreography.Jobs;

public class TenantJobAdmission(ITenantCatalog tenants) : IJobAdmission
{
    public async Task<bool> MayRun(JobAdmissionRequest request, CancellationToken cancellationToken) =>
        await tenants.IsActive(request.JobKey.Namespace, cancellationToken);
}
```

Register it on the silo builder with `AddJobAdmission<TAdmission>()` from `Cratis.Choreography.Hosting`:

```csharp
using Cratis.Choreography.Hosting;

siloBuilder.AddJobAdmission<TenantJobAdmission>();
```

`JobAdmissionRequest` carries the job's key, id, type and request. It is handed only to implementations in the job's own silo, so it is never serialized. Registrations are singletons and are asked in registration order; the first refusal ends the question. Adding the same implementation twice registers it once.

A job asks at these check points:

- `Start`, after the checks that refuse a job that already exists and before anything is written;
- every path into `Resume`: `IJob.Resume`, `IJob.ResumeIfRunning`, `IJobsManager.Resume`, `IJobsManager.ResumeIfRunning`, rehydration after a silo went away, and automatic recovery;
- each later stage of a job with stages, once the steps of the stage before it have reached their outcome.

### What a refusal does

The outcome depends on the job's status:

| The job is | A refusal |
| --- | --- |
| Starting | `StartJobError.NotAdmitted`. Nothing is written and no step is prepared. |
| `Stopped` or being removed, and resumed | `CannotResumeJobError.NotAdmitted`. Nothing is written. |
| `Running` or starting its steps, and resumed, as when a silo went away | `CannotResumeJobError.NotAdmitted`. The job is persisted as `Stopped` with a pending drain, and the drain runs in a turn of its own: nothing is drained or started in the turn that was refused. |
| Between two stages | The job is stopped, the next stage never starts, and `OnStopped` runs once. |

Refusing the resume of a `Running` job whose steps have all completed leaves it `Stopped`, and `OnAllStepsCompleted` never runs. `IJobsManager.Resume` stays a `bool` and answers `false` on a refusal. The drain that follows a refusal can time out like any other; the job then stays `Stopped` with its drain pending until you call `StopAndDrain` or the job is next activated, which completes it. A registered admission is also asked before a rehydrated job resumes: a refusal stops the job, and an exception or timeout leaves it as it was until the next rehydration pass.

### A failure to answer is not a refusal

An implementation that throws, or does not answer within the bound the job applies (below half the Orleans response timeout), has not refused. The operation fails instead: `Start` answers `StartJobError.Unknown` and `Resume` answers `CannotResumeJobError.Unknown`. Nothing changes, so a job that rehydration found running stays `Running` in storage, and rehydration asks again only on its next pass. A job that cannot be asked at its next stage stays `Running`, does not start the stage, and asks again at the same interval until it gets an answer.

With an admission registered, a missing stored request is also an operational failure, not a refusal or permission to run. No policy is called without a request, and a warning identifies the check point without logging a request payload. A later stage stays `Running` and keeps retrying while the request is missing. Without any admission registered, a missing request does not block execution.

### Quiesce jobs in the right order

Admission gates every new execution (`Start`, every `Resume` path, every later stage), and a drain ends the one that is running. An owner that changes what its jobs may do therefore quiesces them in this order: call `IJobs.StopAndDrain` for its running jobs first, then refuse them from `MayRun`. Refusing without draining leaves the running execution to finish, and draining without refusing lets the next resume or stage begin again.

Admission does not cover everything that starts step code. `IJobsManager.RetryFailedSteps`, for a job that opts in with `CanRetryFailedSteps`, moves a job that completed with failures back to `Running` and starts its failed steps without asking, and a step's retry attempts (`MaxStepAttempts`) run inside the execution that was admitted. A stop or removal counts pending attempts as failed. Step work otherwise starts only through the job's fan-out, in the turn of an admitted `Start`, `Resume` or stage; a step never resumes work on its own when it reactivates.

:::caution[`MayRun` runs inside the job's turn]
The job asks from its own non-reentrant turn. An implementation can use storage, memory, and calls to grains that interleave or are stateless workers. It must never call the job it is asked about, through `IJob`, `IJobDrain`, `IJobs` or `IJobsManager`, nor that job's steps, nor a method that does not interleave on a grain that may be waiting for this job's `Start` or `Resume`. The bound turns such a call into a failed operation instead of a hang, but only after the wait.
:::

:::note[Register on every silo that can host the job]
A silo without a registration admits every job, so register the admission on every silo that can host the job types it concerns. A silo on a release without admission (before Cratis.Orleans 1.21.0) does not ask at all, so gate the owner's change on every silo running a release that does. A caller from an older release meets `NotAdmitted` as its default branch, because the values travel as integers.
:::

## Deliver outside input to the job, not its steps

An external worker may call your application back with progress or a result while a step is running. Expose a method on the job's own grain interface, update job state in its implementation, and call `WriteState()`. Override `KeepAfterCompleted` when those values must survive successful completion.

This excerpt adds a callback to an existing job. It assumes your serializable `WorkerProgress` type, `WorkerRequest : IJobRequest`, and `WorkerJobState : JobState` with a writable `LatestProgress` property of type `WorkerProgress`. The job's existing partial class supplies `PrepareSteps` and its other behavior; this is not a standalone job.

```csharp
using Cratis.Monads;
using Cratis.Choreography.Jobs;

public interface IWorkerJob : IJob<WorkerRequest>
{
    Task<Catch> RecordProgress(WorkerProgress progress);
}

public partial class WorkerJob : Job<WorkerRequest, WorkerJobState>, IWorkerJob
{
    protected override bool KeepAfterCompleted => true;

    public Task<Catch> RecordProgress(WorkerProgress progress)
    {
        State.LatestProgress = progress;
        return WriteState();
    }
}
```

The callback handler calls `RecordProgress` through the job's grain reference. `WriteState()` returns a `Catch`, so returning it preserves a persistence failure for the caller to handle rather than acknowledging an input that was not saved. On success the latest progress is stored with the job and survives its successful completion.

This does not update an already-running step's snapshot or wake the step. Coordination with that work belongs in your job's design.

:::caution[Do not use step state as a callback inbox]
Adding step-grain methods that write `State` while `PerformStep` runs is not supported. Such a write can race the step's final status write, and successful completion deletes the stored state. Keep incoming values on the job instead.
:::

## Next steps

- [Stopping and draining](stopping-and-draining.md) explains what a drain guarantees before you refuse a job.
- [Recover jobs after a silo is lost](recovery.md) explains when rehydration asks the admission again.
