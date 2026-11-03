---
title: Job steps and their state
description: How a job step runs as its own grain, what its state keeps, why it may run twice and how to write checkpoints from background work safely.
---

A job spreads its work over steps so that one slow or crashing piece of work cannot take the rest down with it.
Each step is an Orleans grain with its own persisted state, and the job only counts what the steps report. This
page explains what a step owns, how long its state lives and the one rule that keeps its checkpoints correct.

## Write a step

A step derives from `JobStep<TRequest, TResult, TState>` and implements four members. Its state derives from
`JobStepState`, and the interface you register for it derives from `IJobStep<TRequest, TResult, TState>`.

```csharp
public class RebuildShardState : JobStepState
{
    public string Shard { get; set; } = string.Empty;
    public long Position { get; set; }
    public List<string> Pending { get; set; } = [];
}

public interface IRebuildShard : IJobStep<ShardRequest, JobStepResult, RebuildShardState>;

public class RebuildShardStep(
    [PersistentState(nameof(RebuildShardState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<RebuildShardState> state,
    IJobStepThrottle throttle,
    ILogger<RebuildShardStep> logger,
    ISink sink) : JobStep<ShardRequest, JobStepResult, RebuildShardState>(state, throttle, logger), IRebuildShard
{
    protected override Task<Result<PrepareJobStepError>> PrepareStep(ShardRequest request) =>
        Task.FromResult(Result<PrepareJobStepError>.Success());

    protected override ValueTask InitializeState(ShardRequest request)
    {
        State.Shard = request.Shard;
        return ValueTask.CompletedTask;
    }

    protected override async Task<Catch<JobStepResult>> PerformStep(
        RebuildShardState currentState, CancellationToken cancellationToken)
    {
        await sink.Write(currentState.Pending, cancellationToken);
        return JobStepResult.Succeeded(currentState.Shard);
    }

    protected override ValueTask<JobStepResult?> CreateCancelledResultFromCurrentState(RebuildShardState currentState) =>
        ValueTask.FromResult<JobStepResult?>(null);
}
```

- `PrepareStep` validates the request when the job prepares the step. Return a `PrepareJobStepError` to refuse it.
- `InitializeState` runs once preparation succeeded. `PerformStep` never sees the request, only the
  state, so copy what the work needs into it here.
- `PerformStep` does the work. Return `JobStepResult.Succeeded(result)` or `JobStepResult.Failed(...)`. An exception
  thrown from it is caught and recorded as a failed outcome.
- `CreateCancelledResultFromCurrentState` builds a result when the step is stopped before its work ran. Return `null`
  when nothing can be derived.

The step needs the types in `Cratis.Choreography`, `Cratis.Choreography.Jobs`, `Cratis.Choreography.Storage.Jobs`,
`Cratis.Monads`, `System.Text.Json` (for the result excerpt below) and `Microsoft.Extensions.Logging`.
The silo-wide `IJobStepThrottle` is injected so that `JobsOptions.MaxParallelSteps` limits how many step bodies
run at once.

## A step may be performed twice

The job counts a step only once the step has finished with it, so a step that was in flight when the host went
down is performed again on resume. A performer must tolerate being asked twice, and anything that must not happen
twice belongs behind a check the performer makes itself. Progress checkpoints are debounced (see
[Checkpoints are debounced](#checkpoints-are-debounced)), so a crash between checkpoints re-delivers every batch
handled since the last one.

## Step state lasts only as long as the step needs it

Step state supports execution and recovery. It is not a durable place for a step's output or an inbox for
external callbacks. What happens to it depends on the status written:

| Step status | What happens to its stored state |
| --- | --- |
| `CompletedSuccessfully` | Deleted |
| `Removing` (the job is being removed) | Deleted |
| `Failed` or `CompletedWithFailure` | Moved to failed-step storage |
| `Stopped` | Kept so the step can resume |
| `Scheduled` or `Running` | Saved for recovery |
| `Unreachable` (an earlier stage failed) | Saved until the job's state is cleared |

Failed-step storage is a separate collection in MongoDB and in memory; SQL keeps the row with a failed status.
Clearing the job's state removes every step row, failed steps included. Keeping the job with
`KeepAfterCompleted` does **not** keep the state of steps that completed successfully.

A step hands its outcome to the job through `JobStepResult`, not through its stored state. Override
`OnStepCompletedOrStopped` on the job to receive the result of every step that succeeded, failed or stopped, and
copy whatever the job must retain into the job's own state. This excerpt assumes a job state class that derives
from `JobState` and has a `RebuiltShards` list:

```csharp
protected override async Task OnStepCompletedOrStopped(JobStepId jobStepId, JobStepResult result)
{
    if (result.TryGetError(out _))
    {
        return;
    }

    if (result.TryGetFullResult<string>(out var shard, out _, JsonSerializerOptions.Default))
    {
        State.RebuiltShards.Add(shard);
        await WriteState();
    }
}
```

`PerformStep` receives a deep copy of the step's state, taken when the step is marked `Running`. The copy does not
see later writes to the state, and changing it persists nothing. To change state while the work runs, use an
activation callback. The copy comes from Orleans' `DeepCopier<TState>`, which every silo registers through its
serializer. A test host without one, such as an Orleans.TestKit silo, must register it, or the step fails with a
message that names the missing copier.

## Reach the activation through callbacks

`PerformStep` runs in the background, outside the grain's turn. Work that moves a checkpoint forward has to get
back onto the step's own activation. The tempting route is a call through the step's own grain reference, and it
is a trap: while the activation deactivates, Orleans holds that call and later delivers it to a **new**
activation. Your old work then writes state through a grain it no longer owns, after the step stopped.

`TryInvokeOnActivation` is the supported route. The callback runs directly on this activation, also while it
deactivates, and in turn with every other callback and status write of the step:

```csharp
var checkpoint = await TryInvokeOnActivation(async () =>
{
    State.Position = nextPosition;
    await WriteCheckpointDebounced();
});

if (!checkpoint.IsSuccess)
{
    // The run was stopped or replaced: stop working rather than writing anywhere else.
    return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
}
```

A refused callback never ran, and `ActivationCallbackRejected` says why:

| Reason | Meaning |
| --- | --- |
| `NoRun` | The call did not come from the background work of a step run |
| `StaleRun` | The work belongs to an earlier run of this activation |
| `Closed` | The run was stopped, deactivated or timed out and takes no more callbacks |
| `Cancelled` | The optional `queueCancellationToken` was cancelled while the callback waited its turn |

A callback that has started always runs to completion, which is how a final checkpoint written while the work is
being cancelled still lands. The generic overload returns the callback's value.

### Keep callbacks local: capture, call out, commit

A callback holds the step's gate, so it may only change local state and write it. Never call another grain or a
remote system from inside one, never start work it does not await, and never wait inside a callback for a second
callback, because the second waits for the first. When a checkpoint depends on remote work, split it in three:

```csharp
// 1. Capture the inputs on the activation; copy them, do not hand out live state.
var captured = await TryInvokeOnActivation(() => Task.FromResult(State.Pending.ToArray()));
if (!captured.TryGetResult(out var pending))
{
    return JobStepResult.Failed(PerformJobStepError.CancelledWithNoResult());
}

// 2. Do the remote work outside the gate.
await sink.Write(pending, cancellationToken);

// 3. Commit on the activation. This is refused if the run was stopped in between.
var committed = await TryInvokeOnActivation(async () =>
{
    State.Pending.Clear();
    await WriteStateAsync();
});
```

If the commit is refused, the remote write happened but the checkpoint did not, so the work runs again from the last
checkpoint after recovery, the same at-least-once rule as any other crash.

## Checkpoints are debounced

A step reports progress after every batch it hands off, and persisting the whole step document each time would be
costly. `WriteCheckpointDebounced` makes the checkpoint durable once `JobsOptions.StepCheckpointBatchInterval`
batches (100 by default) have been reported. Any other state write, such as a status change on completion, failure
or stop, flushes the pending checkpoint, and so does a timer every `JobsOptions.StepCheckpointFlushInterval`
(5 seconds by default; zero or less turns the timer off) for a step that went quiet mid-range. A step always resumes
from its last persisted checkpoint and reads forward again.

A step whose subscriber cannot make redelivery harmless overrides `CheckpointAfterEveryBatch` to return `true`. That
shrinks the window to the single batch the live delivery path already has. It does not make the step idempotent: a
crash between handing a batch off and writing the checkpoint still delivers that batch again.

## Stopping and removing steps

The framework reports a prepared step that was stopped before or during its work to the job exactly once.
Deactivation without an explicit stop leaves scheduled work recoverable for resume instead of reporting it as
stopped. Do not add cancellation reporting in a step's `PerformWork` override: the framework already reports it,
and reporting it again makes the job count the stop twice.

You can stop or remove a restored job before its steps start again. A fresh step resolves its parent from the
stored job type without starting work. If an ordinary stop cannot resolve the parent, a direct step stop returns
`JobStepError.FailedToReportToJob`, a job notification logs the reason and the step keeps its state. Removal also
preserves the step when the job type is unavailable, storage fails or the lookup deadline expires. Only when
storage definitively reports that the job no longer exists does `Stop(true)` delete the orphaned step without
reporting an outcome, which lets you finish removing a step an interrupted removal left behind. How a stop waits
for work is covered in [Stopping and draining](stopping-and-draining.md), and specifying a step with a test run in
[Test job steps](../testing/job-steps.md).
