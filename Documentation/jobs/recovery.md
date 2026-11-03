---
title: Recover jobs after a silo is lost
description: How rehydration resumes interrupted jobs after a crash, which stops it finishes, and how to tell it every namespace your jobs live in.
---

A silo that crashes mid-job leaves durable `Running` state behind and no live performer. Nothing in Orleans will notice: there is no activation to wake, and Choreography deliberately uses no reminders. Without recovery that job would sit in storage forever. With it, the next silo that comes up, or the survivors of a cluster that lost a member, find the job and run it again.

This page explains what that recovery does, what it will not do for you, and the one piece you must supply yourself: the list of scopes and namespaces your jobs live in.

## What rehydration does

`AddCratisChoreography` installs a recovery participant. It asks the jobs managers to rehydrate once the silo becomes Active, and again when Orleans declares a cluster member Dead.

- **Who recovers.** When you provide a host key catalog, the oldest Active silo (by generation, ties broken by address) requests the pass. Without one, every surviving silo recovers the keys it remembers, because electing one would miss keys only another survivor knows. Briefly disagreeing membership views can trigger duplicate passes; live steps are checked rather than started twice.
- **When it fails.** If provider construction, a provider's `GetKeys`, or a manager's `Rehydrate()` fails, recovery retries only the failed providers and manager keys after 1 second, doubling the delay up to 60 seconds, until they succeed or the silo stops. Retries need no new membership change and do not block membership-triggered passes.
- **What it does to a job.** Recovery keeps the original job id, scope, namespace and current stage. It starts only unfinished prepared steps; successful steps stay completed. A job interrupted while preparing continues from its saved plan, and one with no plan fails to resume with `CannotResumeJobError.MissingStepPlan` (inspect or remove it through your job management).

`IJobsManager.Rehydrate()` waits for dead-job cleanup and for loading the interrupted jobs, then returns before all jobs resume. The manager drains that snapshot oldest first, up to `JobsOptions.MaxConcurrentRehydration` (default 8) at a time, as background work on its activation. Overlapping calls share preparation and the drain.

Before dispatching each job the manager reloads it. A job that is `Running`, `PreparingJob`, `PreparingSteps` or `StartingSteps` is resumed; a job that is `Stopped` or `Removing` is finished (below), never resumed. An explicit `Resume()` can still resume a stopped job.

The manager keeps its activation alive while the drain runs. That prevents idle collection, not requested deactivation, migration, memory-pressure shedding or silo shutdown. Jobs not yet resumed stay in storage and resume on the next `Rehydrate()` call or host startup. The drain is not a durable queue or a cluster-wide concurrency limit.

:::caution[An interrupted step may run twice]
Recovery cannot know whether the step finished its external effect before the silo died. Protect external effects with idempotency or receiver-owned deduplication. A durable `Stop()` is the exception: automatic recovery treats it as stopped work, not as interrupted work.
:::

## Tell recovery which namespaces exist

Storage scopes and namespaces are host-owned coordinates, and recovery cannot infer them from arbitrary database-name resolvers. Its default `JobsManagerKeys` provider remembers the managers activated, and the keys started through `IJobs`, on this silo. It logs a startup warning when no host provider is present: recovery is then incomplete for a survivor that never served a key, and the in-memory catalog does not survive a process restart.

Provide a singleton `IJobsManagerKeys` implementation on every silo for complete recovery. Providers are found by type scanning, combined with the default, and duplicate keys are recovered once per pass. They run outside any request scope, so never inject tenant-scoped databases, request-scoped services or a current-user value. A provider failure is logged without stopping the other providers, providers are resolved again on each pass, and a provider that failed to construct is retried without another membership change. Register them as singletons to keep their intended lifetime. There is no periodic sweep; `IJobsManager.Rehydrate()` remains available for an explicit pass.

:::note[The workflow engine registers its own keys]
When `Workflows` is enabled, `WorkflowJobsManagerKeys` supplies the keys of every workflow scope, and it counts as a host provider, so the incomplete-recovery warning stops. It covers only the wave jobs of workflows, not jobs you start yourself.
:::

For a host that serves many event stores and tenant namespaces, the catalog is the set of event stores times tenant namespaces. This example uses a host-owned configuration snapshot; a dynamic host can enumerate its authoritative catalog in `GetKeys` instead. `JobRecoveryCatalog` is an application type, not part of Choreography:

```csharp
using Cratis.Choreography.Jobs;

public sealed record JobRecoveryCatalog(
    IReadOnlyDictionary<string, IReadOnlyList<string>> TenantNamespacesByEventStore);

public sealed class StudioJobsManagerKeys(JobRecoveryCatalog catalog) : IJobsManagerKeys
{
    public Task<IEnumerable<JobsManagerKey>> GetKeys(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var keys = catalog.TenantNamespacesByEventStore.SelectMany(store =>
            store.Value.Select(tenantNamespace => new JobsManagerKey(store.Key, tenantNamespace)));
        return Task.FromResult(keys);
    }
}
```

In your existing host setup, before `builder.Build()`, register the snapshot and the provider as singletons:

```csharp
builder.Services.AddSingleton(new JobRecoveryCatalog(
    new Dictionary<string, IReadOnlyList<string>>
    {
        ["studio"] = new[] { "tenant-a", "tenant-b" },
        ["verification"] = new[] { "tenant-a" }
    }));
builder.Services.AddSingleton<StudioJobsManagerKeys>();
```

These are the scope and namespace pairs you pass to `GetJobsManager`, not database names. Include a default namespace explicitly if your application uses one. For a separately configured silo, call `AddCratisChoreographyJobsRecovery()` from `Cratis.Choreography.Hosting` after registering the job system and its storage; `AddCratisChoreography` already does this.

## Stops that a silo could not finish are finished

A job persists `Stopped` or `Removing` before its steps report, and runs `OnStopped` only once they all have. A silo that went away in between left a stop no one would finish. Rehydration activates every such job: steps that never recorded that they stopped are told to stop again, `OnStopped` runs once they have, and a job being removed is then removed. Steps in a stage that never started have nothing to stop and are left alone. Nothing resumes; the stop stands.

The job records in `JobState.StopFinalized` that its stop is finished, and rehydration leaves it alone afterwards. A custom job storage provider must persist that property. A job stopped by a release without it reads as not finalized, so `OnStopped` runs once more for it on the first rehydration after an upgrade, and your hook must tolerate that.

Rehydration reads jobs through `IJobStorage.GetJobsExcludingFinalizedStops`, which leaves out stopped jobs whose stop is finalized, so a pass does not materialize every job ever stopped. The MongoDB provider filters in storage, as do the SQL providers for PostgreSQL, SQL Server and SQLite; the in-memory provider filters before building its result. A custom provider keeps working unchanged, because the interface default reads `GetJobs` and filters the result at the cost the read had before. Override it to filter in storage, leaving out only jobs that are `Stopped` with `StopFinalized` set; a `Removing` job stays in the result.

Upgrading from a release that predates stop finalization (before Cratis.Orleans 1.26.0) has one-time costs. The first rehydration activates every stored stopped job once, runs `OnStopped` and records the finalized stop, bounded by `MaxConcurrentRehydration`; later passes skip those jobs. MongoDB builds the `status_stopFinalized` index in the background the first time the jobs storage reads, and no SQL migration is needed.

## A removal finishes even when a report fails

A step being removed deletes its own record once the job has stored its stop. Until then it keeps the record and reports the stop again, waiting `JobsOptions.StepDeliveryRetryDelay` before the second report and doubling the wait up to `MaxStepDeliveryRetryDelay`. Unlike other outcomes, this report is not limited by `StepDeliveryMaxAttempts`; it continues while the step's activation lives, and a job that no longer exists answers that there is nothing left to report.

The job counts each step's stop once per execution and records the steps it counted in `JobState.StoppedStepsCounted`, so a report that arrives again in a later activation is not counted twice. A custom storage provider must persist that property too. A step can have a stop from an earlier execution and a run of the current one, and each step still counts once. If the earlier stop was counted first and the current run then succeeds or fails with no attempt left, the job takes the stop back and counts the outcome in the same write. If the earlier stop was counted while the current run may still be running, the job tells that run, once, that the job is removed; the run stops, reports a stop that is already counted, and deletes its own record.

When finishing the removal fails (`OnStopped` throws, clearing the job fails, or saving the count fails), the job answers the step's report with an error so its next report runs the finish again. Once every step has reported, the job also retries the finish itself on a timer: after 1 second, doubling up to 60 seconds, until the job is cleared. The timer keeps the activation alive and uses no reminders, and if the silo goes away, rehydration finishes the removal. Calling `Remove()` on a job already being removed runs the finish again once no step is left to report, and answers `RemoveJobError.JobIsAlreadyBeingRemoved`, or `RemoveJobError.Unknown` when the finish failed again. `OnStopped` can therefore run more than once for a removal whose finish failed, and a job whose `OnStopped` always throws keeps its activation and retries every minute.

## Leases and stop reports

With `JobsOptions.StepLeaseTimeout` set, a running step records a lease (its silo, activation and last heartbeat) and renews it on a state write or the checkpoint flush timer once a third of the lease timeout has passed since the last heartbeat. The watchdog restarts a step only when its owner silo is no longer part of the cluster and its lease has expired. A step without a lease, written by a release that did not know them, is left alone for one lease period after its last change and then started again, which is harmless for a step that is still running. Override `Job.GetActiveSilos` to change how cluster membership is read. The default is zero: leases are off.

`Job.Stop` answers `StopJobError.CouldNotPersistStop` when the stop could not be written, and leaves the job as it was. `IJobsManager.Stop` returns that result instead of discarding it.

## Known limitation

A lost acknowledgement of a `Running` write can leave a job idle after step-delivery retries are exhausted. The retained plan prevents data loss but does not schedule another activation. An explicit `Rehydrate()`, a silo restart or a membership change recovers it. Durable reconciliation is tracked as issue 177 in the Choreography repository.

## Next steps

- [Stages, failures and retries](stages-failures-and-retries.md) covers the watchdog that finalizes and re-drives running jobs.
- [Stopping and draining](stopping-and-draining.md) covers what a stop guarantees before recovery has to finish it.
- [Host the silo](../hosting/index.md) shows where recovery is registered.
