---
title: Stages, failures and retries in jobs
description: Order dependent steps in stages, see how a failed step is recorded, bound active steps and retry failed attempts, with the watchdog that settles lost reports.
---

Most jobs are a pile of independent pieces of work, and the useful questions are about what happens when the pile
is large, when one piece fails and when a report goes missing. This page covers the ordering you can add
(stages), what a failure does to the job, and the options that bound and heal a running job.

## Steps are independent by default

Every step of a job starts at once and the silo-wide throttle decides how many run. That is the shape to prefer:
it needs no coordination and spreads over every silo that hosts the step. A job starts and resumes at most
`JobsOptions.MaxConcurrentStepStarts` steps at a time (16 by default) so a burst of activations does not exhaust
the storage connection pool.

## Stages order steps that depend on each other

Some jobs are pipelines, where a later step would do the wrong thing, and not merely a slow one, if it ran before an
earlier one. Such a job puts its steps in **stages** by passing a stage to `CreateStep`:

```csharp
protected override Task<IImmutableList<JobStepDetails>> PrepareSteps(RebuildIndexRequest request) =>
    Task.FromResult<IImmutableList<JobStepDetails>>(
    [
        CreateStep<IWarmCaches>(new ShardRequest(request.Index, string.Empty), stage: 0),
        ..request.Shards.Select(shard => CreateStep<IRebuildShard>(new ShardRequest(request.Index, shard), stage: 10)),
        CreateStep<IPublishIndex>(new ShardRequest(request.Index, string.Empty), stage: 20),
    ]);
```

- Every step in a stage reaches an outcome before any step in a later stage starts. Within a stage the steps run in
  parallel exactly as without stages. A job that never assigns one has every step in `JobStepStage.First`, which is
  a single stage and the plain fan-out.
- Stages run in the order of their value, and gaps are harmless, so numbering by tens leaves room to insert one.
- A stage is a **barrier** by default: if any of its steps fails, the job stops there. The steps in every later
  stage are recorded as `Unreachable`, count toward completion, and the job ends `CompletedWithFailures` instead of
  waiting forever for steps that will never run.
- Override `GetFailureBehaviorFor` to return `JobStageFailureBehavior.ContinueWithNextStage` for a stage that is
  best-effort:

```csharp
protected override JobStageFailureBehavior GetFailureBehaviorFor(JobStepStage stage) =>
    stage == 0 ? JobStageFailureBehavior.ContinueWithNextStage : JobStageFailureBehavior.StopJob;
```

- Every step is still prepared up front, so preparing a step in a later stage cannot observe what an earlier stage
  did. Read anything it needs from an earlier stage when the step is performed.
- `Start` returns once the first stage is running and its errors describe that stage. When no step of the first
  stage starts but that stage continues on failure and a later stage starts, `Start` returns
  `FailedStartingSomeJobSteps`, because the job is running. Later stages report start failures through the job.
- Each later stage is a new beginning of work, so the job asks its [admission](admission-and-external-input.md)
  owner again before starting it.
- On resume the job picks up at the stage it had reached, and the stages behind it wait their turn. After a cold
  activation, a stage reached before `OnBeforeResumingJobSteps` has succeeded waits until it has, and the
  watchdog's resume starts it.

Stages only exist when a plan has more than one distinct stage value; a single value is the plain fan-out.
`RetryFailedSteps` is not available for a job with stages.

## One failed step does not abandon the rest

A job that could not do one piece of work has still done the others, and its history says which was which. A step
fails by returning `JobStepResult.Failed(...)` or by throwing; either way the failure is recorded state, not an
exception on anybody's startup path. The job counts it in `Progress.FailedSteps`, finishes the remaining steps,
and ends `CompletedWithFailures`. A failed step fails the job's completion status and never fails the application.
The job record is kept by default (see [what a job remembers](index.md#what-a-job-remembers)).

### Counted outcomes survive reactivation

After a resume or `RetryFailedSteps` reopens an execution, the job records the exact step identities whose
terminal outcomes it counts in `JobState.CompletedStepsCounted`. It saves the record with progress and keeps it
across later resumes and activations. A delayed stop from an earlier execution then cannot count the same step
again during removal, even after a successful step deleted its own state. Retrying a counted failure removes that
step's entry along with its failed count, and its new outcome is recorded when counted. Stops are tracked
separately in `StoppedStepsCounted`.

A job that never resumed or reopened records an empty list. For resumed or reopened jobs the list grows with
counted outcomes and has no hard limit. Measured serialization overhead is about 27.89 bytes per entry in BSON and
39 bytes in JSON. At 100,000 entries the job logs a warning and keeps recording, and a large resumed job can
approach MongoDB's 16 MiB document limit or another provider's limit, especially when its other state is large.

Custom storage providers must persist and restore `CompletedStepsCounted` and default it to an empty list for older
state. During a rolling upgrade, a silo that does not know the member may drop it when it rewrites a job, and
outcomes dropped that way cannot be rebuilt. The protection applies to outcomes recorded and kept by silos that
know the member.

## Keep a window of active steps

A job with thousands of steps should not activate all of them at once. Set `JobsOptions.MaxActiveStepsPerJob`, or
override `Job.MaxActiveSteps`, to start that many steps and start the next one each time a step reaches an
outcome. Values below 1 are treated as 1. Steps that have not started stay prepared in storage. A job that resumes
after a restart rebuilds its window from storage and starts the steps that were running first. With stages, the
window applies to the stage that is running. Stopping a job counts the steps that never started as stopped, and
they start again when the job resumes.

## A step persists its outcome, then delivers it

A step writes its terminal status first and then delivers it to the job, retrying with a growing delay until the
job acknowledges it:

| Option | Default | Meaning |
| --- | --- | --- |
| `StepDeliveryMaxAttempts` | 10 | Deliveries before the step leaves it to the job's watchdog |
| `StepDeliveryRetryDelay` | 1 second | Delay before the second delivery; each further one doubles it |
| `MaxStepDeliveryRetryDelay` | 30 seconds | The longest delay between two deliveries |

A delivery that never succeeds does not mark the step failed: the persisted outcome stays and the watchdog
accounts for it. The job accepts an outcome once per step and attempt, so a delivery repeated after a lost
acknowledgement is not counted twice. A step being removed is not limited by the attempt count, because it deletes
itself once the job has stored its stop.

## The watchdog settles reports that never arrived

The counters a job keeps while steps report to it are a cache, and storage is the record. Every
`JobWatchdogInterval` (1 minute by default, and soon after a job activates as `Running`) a running job counts its
steps by status in storage. When every step is done, it recounts its progress from storage and completes; `Failed`
and `CompletedWithFailure` count as failed, and a step that succeeded is no longer in storage. The watchdog also
retries steps whose start threw during resume, fills the active window and, with leases on, restarts steps whose
owner is gone (see [Recovery](recovery.md)). A zero, negative or infinite interval turns it off, and then nothing
retries.

`IJobStepStorage.CountByStatus` is a default interface member, so a third-party storage keeps compiling and can
override it with a cheaper query.

When `OnBeforeResumingJobSteps` throws for a job that is `Running`, the job stays `Running` and the watchdog
retries the resume on the next tick, then skips 1, 3, 7 and then 15 ticks between attempts. Until the hook
succeeds in this activation, the watchdog fills no window, restarts no step whose lease expired and starts no next
stage, and pending step retries wait without consuming attempts. Once it succeeds, retries re-arm for their
original due times. A job reverted to `Stopped`, or stopped later, is never resumed by the watchdog. A job
interrupted while `PreparingJob`, `PreparingSteps` or `StartingSteps` with a saved plan is retried the same way and
finishes startup through `Resume`.

## Retry failed attempts

`JobsOptions.MaxStepAttempts` (1 by default, so a step never retries) lets a failed step run again. The step
records the failed attempt, the job decides whether to retry through `Job.ShouldRetryStep(stepId, result,
attempt)` and schedules the next attempt after an exponential backoff:

| Option | Default | Meaning |
| --- | --- | --- |
| `StepRetryBackoff` | 5 seconds | Delay before the second attempt; each further attempt doubles it |
| `MaxStepRetryBackoff` | 5 minutes | The longest delay between two attempts |

Override `Job.GetStepRetryDelay(failedAttempt)` to compute the delay yourself, and `ShouldRetryStep` to retry a
timeout but not a rejected input. A retried failure is not counted as a failure until its attempts run out. While
the resume hook is owed, due attempts wait, and a stop or removal counts pending attempts as failed.

:::caution[The retry schedule lives in the job's memory]
After the job is reactivated, a failed attempt that was waiting for its retry counts as final. Use retries for
transient errors, not as a durable queue.
:::

## Run the failed steps again

Resuming a job that completed with failures does nothing. A job type that wants its failed steps run again
overrides `Job.CanRetryFailedSteps` to return `true`, and callers use `IJobsManager.RetryFailedSteps(jobId)`. The
job runs each step that completed with failure as its next attempt and goes back to `Running`. The call returns the
number of steps run again, or a `RetryFailedStepsError`: `NotSupported`, `JobIsNotCompletedWithFailures` or
`CouldNotPersist`. Jobs with stages do not support it.
