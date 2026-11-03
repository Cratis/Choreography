---
title: Plan a job before it runs
description: How a job saves its step plan before any work starts, validates it, keeps large plans out of the job document and gates initialization before dispatch.
---

If a silo dies while a job is starting, recovery has two choices: plan again or reuse the plan it had. Planning
again produces new step identities, so steps that already ran would run a second time under different names and
the progress a person was watching would reset. A job therefore plans first, **saves the complete plan**, and
only then prepares and starts steps. Recovery reuses the saved plan and never calls `PrepareSteps` again.

## Write the plan

`PrepareSteps` says what the job will do before any of it happens, which is what lets somebody watch progress
instead of a spinner: the total is known up front and each step reports its own outcome as it finishes.
`CreateStep<TJobStep>` creates the step's identity, key and result type for you. This excerpt belongs in a job
class whose request carries a list of shards:

```csharp
protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(RebuildIndexRequest request) =>
    Task.FromResult<IImmutableList<JobStepDetails>>(
        [..request.Shards.Select(shard => CreateStep<IRebuildShard>(new ShardRequest(request.Index, shard)))]);
```

The request type implements `IJobRequest`. Mark it and any step request with `[GenerateSerializer]` and `[Id]`
members so Orleans can carry it between silos.

## What the saved plan contains

The plan holds every step's identity, stage, grain type and preparation request. The job writes it before
preparing any step, and the write that makes the job `Running` removes the inline copy, so later progress writes
and job listings do not carry every request. Dead-job cleanup never reaps a job that has a saved plan, including
an empty plan, however long an outage lasted.

Preparation requests must round-trip with the host's `JsonSerializerOptions`, or with the default Cratis JSON
converters when none are registered, because the plan keeps each request's concrete type and JSON payload. `Start`
reads each request back and compares its JSON. A request that does not survive the round trip returns
`StartJobError.InvalidStepPlan` before any step is prepared. The check does not prove semantic equivalence for
arbitrary polymorphic members, so configure converters for those request types.

A step does not see its original request while it performs; it sees its state. Copy what the work needs into the
state in `InitializeState` (see [Steps](steps.md)).

## Decide when steps start

By default `Start` prepares and starts the job's steps before it returns, so its result describes how that went.
A job whose step count scales with the size of the data, such as one step per event source, bills the whole burst
of activations and writes to whoever called `Start`. Such a job overrides `StartStepsInBackground` to return
`true`. `Start` then returns as soon as the job is durable and the steps come up behind it. The trade is that
`Start` can no longer say whether the steps started: failures are logged and handled by the job.

## Initialize before dispatch

A silo can stop during step preparation, or inside your start hook, before initialization finishes. Override
`OnBeforeDispatchingJobSteps(JobStepDispatchContext context)` for initialization that must be ensured before
steps run, on both start and resume. The default does nothing. If your override fails, that dispatch starts no
steps.

```csharp
protected override Task OnBeforeDispatchingJobSteps(JobStepDispatchContext context) =>
    context.InitializationMayBeIncomplete ? EnsureStagingArea() : Task.CompletedTask;
```

The hook runs after `OnBeforeStartingJobSteps` on a clean start and after `OnBeforeResumingJobSteps` on a
resume. Those hooks keep their roles, and recovery never replays the start hook, even if the silo stopped before
it ran. Resuming between two [stages](stages-failures-and-retries.md) also runs the dispatch hook before the
next stage starts.

`InitializationMayBeIncomplete` is `true` when recovery finds the job in `PreparingSteps` or `StartingSteps`.
Recovery then prepares only the steps whose preparation was not saved, with their original identities, and ensures
initialization before dispatch. The flag is `false` on a clean first start and on an ordinary resume of a running
or stopped job, which crossed the initialization barrier before reaching `Running`. It is not an exactly-once
guarantee: make the override idempotent, using a durable token of your own when initialization has external effects.

A job interrupted before its plan was saved, or an older job left preparing or starting without one, returns
`CannotResumeJobError.MissingStepPlan` from `Resume` instead of inventing new step identities. A fresh activation
never treats a persisted starting or running status alone as proof that the job is already running.

## Large plans stay out of the job document

A plan whose whole serialized form (requests, identities, types and stages) is larger than
`JobsOptions.MaxInlineStepPlanSize` (1 MiB by default) would make the job document too large for some stores.
Where the storage provider can keep a plan separately, the job saves it there instead, one small document per
step. MongoDB and the in-memory provider can; SQL has no limit that needs it and keeps the plan inline. A value of
zero externalizes every plan whenever that is possible.

The job document records `HasStoredStepPlan` and `StoredStepPlanIncarnation`. Each external publication gets a
fresh incarnation, persisted in non-terminal job state before any plan document is saved, so reuse of a supplied
job id can never load another publication's plan. At `Running` the reference is cleared but the stored plan stays
until a durable terminal state (including a finalized stop) or job removal. A job that never externalized a plan
does not contact plan storage on completion or removal, and a failed plan deletion is logged without failing
completion. Cleanup deletes a plan only when its job is missing, durably terminal or a finalized stop, and only
the incarnation it read.

MongoDB inserts large plans in bounded batches and retries a partial save by deleting only that incarnation's
earlier attempt before inserting again. A `jobId_incarnation_index` index supports ordered, fenced reads; it is
created idempotently and an older jobId-only index is left in place.

### Plans during a rolling upgrade

Every job class advertises `JobStepPlanStorageSupportAttribute` in the cluster manifest. A job externalizes its
plan only when every silo that hosts the job class advertises it. While a silo without the capability is in the
cluster, the plan stays inline whatever its size, and a plan too large for the job document fails the job cleanly
with a recorded reason. Nothing needs configuring: once the last older silo has left, new jobs externalize large
plans, and a job started earlier keeps its inline plan.

Drain jobs that use externalized preparation, and let them finish, before rolling back. An older silo cannot
resume externalized preparation: it does not understand the reference and may drop it when rewriting the job.
Plans saved without an incarnation by an earlier external-plan format are not loaded or reaped automatically;
drain or repair those jobs through your own recovery procedure. See
[Mixed-release clusters](../upgrading/mixed-release-clusters.md) for the whole rollout.

## When start fails

| Result | What happened to the job |
| --- | --- |
| `InvalidStepPlan` | A request could not round-trip; the job is `Failed` with the reason and no plan |
| `CouldNotPrepareJobSteps` | A step refused or failed preparation; the job is `Failed` and `OnFailedToPrepare` runs |
| `NoJobStepsToStart` | The plan was empty; the job completes immediately |
| `Unknown` | An unexpected error, or a write that was not durable; the job is written as `Failed` where it can be |

A job whose start or plan persistence fails is written as `Failed` with the reason and without its plan, instead of
staying in preparation. If that write also fails, the job deactivates and keeps its stored plan for recovery.

:::caution[Repairing a job stuck in preparation]
A `PreparingJob` job without a saved plan cannot safely plan again. Remove it through
[the jobs service](manage-jobs.md), or wait for dead-job cleanup after `JobsOptions.DeadJobThreshold`. Never delete
a live job that has a saved plan. Deleting a job does not reset state owned by your own observers, so release
those through your application's recovery procedure first.
:::

## Known limitation

A lost acknowledgement of the write that makes a job `Running` can still leave the job idle once step-delivery
retries run out. The retained plan prevents data loss but does not schedule another activation. Explicit
rehydration, a silo restart or a membership change recovers it. Durable reconciliation is tracked in
Cratis/Choreography#177.
