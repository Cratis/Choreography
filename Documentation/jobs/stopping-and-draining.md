---
title: What stopping and draining guarantee
description: What a stopped job step has finished doing, how long a stop waits, and how a resume or a failing hook interacts with a stop in flight.
---

Cancelling work is easy to start and hard to finish. A step in the middle of a call to an external service has no way to take that call back, a callback may still be on its way to the step's grain, and a job that stops and resumes within seconds can meet a step that has not finished stopping yet. If the framework only flipped a status, you would read half-written checkpoints and, after a resume, count a step's old stop against its new run.

This page explains what the framework waits for, which guarantees it gives at each point, and which it does not. To stop, resume or drain a job, see [Manage jobs](manage-jobs.md).

## Stopping waits for the work

Stopping a step, or deactivating its grain, cancels the work and then waits for it, and for any callback it has started, before anything else happens. One deadline covers the whole wait.

| Trigger | Waits up to | Then |
| --- | --- | --- |
| `IJobStep.Stop` | `JobsOptions.StepStopTimeout` (10 seconds) | Starts reporting exactly one outcome to the job and answers `Success`. After the deadline it answers `StopTimedOut` and reports the outcome once the work settles. |
| The job stops or is removed | Nothing: the job is answered as soon as cancellation is requested, so it never waits on its own step | Reports exactly one outcome once the work settles. A removed step is then removed from storage. |
| Deactivation, such as a graceful silo shutdown | Orleans' deactivation timeout | Writes no final status and reports nothing. The step stays `Scheduled` or `Running` for recovery. |

A second stop, or a removal, that arrives while a stop is still in progress joins it, so the job hears about the step once. If the job's notification to a step fails, the job retries it after 1 second, doubling the delay up to 60 seconds, for as long as the job stays stopped or being removed.

The outcome is the one the work produced. If the work finished successfully at the moment the stop came in, the step reports success, not a stop. The order of writes does not change: a failure or stop is reported to the job first and persisted once the job accepts it, and success is persisted first and then reported.

:::caution[A timed-out stop is not quiescent]
When the work does not settle in time, `Stop` answers `JobStepError.StopTimedOut` and has recorded nothing yet. The work may still be running; from then on its callbacks are refused with `Closed`, and I/O it already issued cannot be called back. The stop goes on in the background: once the work settles, the step reports itself stopped and records it, exactly once. Treat a timed-out step as not stopped until then, and keep `StepStopTimeout` below the Orleans response timeout so the caller sees this answer rather than a timed-out call.
:::

## Limits that remain

- A step deactivated while its job keeps running is not started again until recovery or `Resume()`.
- A report already sent to the job when the activation deactivates is not withdrawn, and the step persists nothing for it afterwards. After a graceful shutdown the job may therefore have accepted a stop or failure while the step's stored status is still `Running`; recovery runs the step again. Execution generations (below) do not change this, because the run that recovery starts belongs to the same execution.
- A step whose work still calls its own grain reference, instead of `TryInvokeOnActivation`, makes deactivation wait for that work up to Orleans' deactivation timeout, because the held call cannot complete until the activation is gone. Move such calls to `TryInvokeOnActivation`; [Steps](steps.md) explains why.

A step that a [drain](#a-stop-and-a-drain-differ) accounted for is not affected by the second gap: the drain persists its outcome before it answers.

## A stop and a drain differ

`Stop` is accepted as soon as the job has persisted it; its steps finish stopping afterwards. `StopAndDrain` persists the job as `Stopped` together with a pending drain, asks each step that can still run to stop, claim its outcome, persist it and answer, and returns only when every step has done so or the timeout passes. A step counts as drained only by its own answer.

When a drain returns `Drained`, no step's work or activation callback runs any more, every step's outcome and last checkpoint is in step storage, the stop hooks have run, and the job's state is persisted. It does not cover work that your hooks or step work start without awaiting it, nor calls already on their way from step work to your own grains. Stamp such calls with a revision of your own so the receiver can tell a late one apart. [Manage jobs](manage-jobs.md) shows how to call it and read its result.

The pending drain survives the job's silo. If that silo goes away mid-drain, the job completes the drain when it is next activated, and `Resume()` refuses it with `CannotResumeJobError.JobCannotBeResumed` until then. Hooks can therefore run twice for the same step, so `OnStepCompletedOrStopped` and `OnStopped` must tolerate it.

A crash can also leave `OnStepCompletedOrStopped` never running for a step. A successful step whose report did not reach the job before it went away has no record left, so the drain neither asks it nor finds its outcome in step storage. A step that persisted its stop and answered a drain before the job went away answers the completing drain from a fresh activation, which no longer has the outcome to hand over. Nothing of the job runs in either case; read a step's result from your own records, not from the hook alone.

## A resume does not mistake an earlier stop for its own

A job can be resumed while one of its steps is still stopping. Without help, the step would refuse the new start as already started, and its late stop report would reach the resumed job, which would count it and could even complete on it.

A job therefore tells its executions apart. An execution begins when the job starts and each time it resumes from a stop or removal; recovering a job left running continues its execution. The job sends the execution's generation with every step start and stop, and a step reports its outcome with the generation of the run that produced it. When a resume overtakes a pending stop:

- the step accepts the new start but defers it until the stop has reported, then runs it in a turn of its own;
- the job ignores the late stop report, keeps the step tracked and starts it again in case the deferred start was lost, which is safe because starting is idempotent;
- a late stop notification from the earlier execution does not stop the resumed run.

A success or failure from an earlier execution still counts, once. While the job tracks the step, it counts as reported. A resume that reloads the steps from storage no longer tracks a step whose success was persisted, because that removes the step's record. For such a report the job recounts its progress from step storage, which has the outcome, and moves its stages on. It does not run `OnStepCompletedOrStopped` for that report.

Generations need every silo to run a release that carries them (Cratis.Orleans 1.20.0 or Cratis.Choreography 2.0.0 and later). A step or job on an older release sends and reports none, and keeps the earlier behavior.

## A failed resume hook can leave a stopped job ready to retry

A resume hook can reject work that is not safe to run yet. If the job entered `Resume()` as `Stopped`, had no pending drain, and had already accounted for every worker, a failure in `OnBeforeResumingJobSteps` restores its stopped progress and stage flags and persists another `Stopped` status. No steps start and no drain begins. `OnStopped` runs again, so it can release whatever the failed hook began. `Resume()` reports failure, and a later call can retry the hook.

Eligibility uses the pre-resume `JobProgress.IsStopped` accounting, not proof that every worker is quiescent. Use `StopAndDrain` when you need that guarantee; a stopped status or completed accounting alone is not a drain acknowledgement.

The job still persists `Running` before calling the hook. A consumer coordinating its own replay entry may need to see that status to adopt the job without resuming it recursively. Drain accounting is kept until the hook succeeds, so a late report cannot count an already drained outcome twice during the failed attempt.

The compensation is deliberately limited. A stop whose workers have not all reported, a removal, or recovery of a job already `Running` or `StartingSteps` may still have live work, and a hook failure does not roll those back or drain them. Nor does it cover a failure after step dispatch begins, when some steps may already be running. For a job left `Running`, a later manual `Resume()` or a watchdog retry can recover steps stopped before that execution, provided no stops have been counted in it. The retry keeps the same execution generation and preserves live outcome tracking, without reviving accepted terminal outcomes or interrupting pending retries. Recovered steps start only after the resume and dispatch hooks succeed, within the current stage and concurrency window.

If the resume write or the compensating write fails, the activation of the eligible stopped job retires rather than serve an uncertain state, and recovery reads whichever status reached storage. A crash after `Running` was written but before compensation can also leave that status persisted; cold recovery can then retry the hook and the stopped steps from the earlier execution.

None of this makes a permanently failing hook succeed. Inspect the failed resume and correct its cause before retrying. A job left `Running` is retried by the watchdog (see [Stages, failures and retries](stages-failures-and-retries.md)); a job reverted to `Stopped` is not, and needs an explicit resume.

## Next steps

- [Manage jobs](manage-jobs.md) covers starting, stopping, deleting and draining through `IJobs`.
- [Recover jobs after a silo is lost](recovery.md) covers how stops that their silo could not finish are completed.
