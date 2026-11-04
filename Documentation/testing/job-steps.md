---
title: Test job steps with BeginTestRun
description: Specify a job step's own work with BeginTestRun, including callbacks on the activation, stopping, closing admission and what a test run does not record.
---

A step's work runs off the grain's activation. The only way it reaches the grain is `TryInvokeOnActivation`, and that call needs a run: without one, every callback is refused with `ActivationCallbackRejected.NoRun`. In a real silo the run is created for you. In an [Orleans TestKit](index.md) silo nothing calls `PerformStep`, so a spec for the step's own work would find every checkpoint refused.

`BeginTestRun` gives a step hosted in a specification the run it has in a silo, so you can specify its work, its callbacks and its behavior when stopped.

## Run a step under test

Host the step with `TestKitSilo`, register what the step needs, begin a run and perform it:

```csharp
var silo = new TestKitSilo();
silo.AddService(Options.Create(new JobsOptions()));
silo.AddService<IJobStepThrottle>(new PassThroughThrottle());
silo.AddService<ILogger<CountingStep>>(NullLogger<CountingStep>.Instance);

var key = new JobStepKey(Guid.NewGuid(), "event-store", "namespace");
var step = await silo.CreateGrainAsync<CountingStep>(Guid.NewGuid(), keyExtension: key);

var run = step.BeginTestRun();
var performed = await run.Perform(new CountingState { Items = 3 });
await run.CallbacksSettled;
```

This excerpt assumes your own `CountingStep`, a `JobStep<int, int, CountingState>` whose `PerformStep` posts one `TryInvokeOnActivation` callback per item, and a `PassThroughThrottle` test double implementing `IJobStepThrottle` with an empty `AcquireAsync` and `Release`. The framework's own specs register the same services; the persistent state storage for `[PersistentState]` is supplied by TestKit.

`Perform(state)` calls your `PerformStep` off the activation with the state you pass and the run's `Cancellation` token, and returns the `Catch<JobStepResult>` your step returned. Callbacks the step posts with `TryInvokeOnActivation` are admitted and run one at a time on `run.Activation`, a stand-in scheduler. When `PerformStep` returns, `Perform` closes admission, as a drain does.

`BeginTestRun` is an extension method on `JobStep<TRequest, TResult, TState>`. It takes an optional `TaskScheduler` for the stand-in activation and defaults to a new exclusive scheduler.

## What the run exposes

| Member | What it tells or does |
| --- | --- |
| `Activation` | The scheduler standing in for the activation. Callbacks run on it and nothing else does. |
| `IsOnActivation` | Whether the code calling it is running on `Activation`. Use it to check that state is only touched from a callback. |
| `Cancellation` | The token `PerformStep` receives. |
| `AdmissionOpen` | Whether the run still admits callbacks. |
| `CallbacksSettled` | A task that completes when no admitted callback is running. Await it before asserting on state a callback may still be writing. |
| `Perform(state)` | Performs the step off the activation and returns what it returned. |
| `Stop()` | Cancels the work and keeps admission open. |
| `CloseAdmission()` | Refuses callbacks that have not started. Does not cancel the work. |

## Specify stopping and giving up

A step ends in two separate moments, and a step that checkpoints should be specified at both.

| Call | Effect on the run | What your step sees |
| --- | --- | --- |
| `run.Stop()` | Cancels the work, admission stays open | `Cancellation` is cancelled; a final checkpoint through `TryInvokeOnActivation` still lands |
| `run.CloseAdmission()` | Refuses callbacks that have not started, and any posted afterwards | Callbacks come back as `ActivationCallbackRejected.Closed`; the work is not cancelled |

`Stop()` is what an ordinary stop does while it waits for the work to finish. `CloseAdmission()` is what happens once that wait passes its deadline, or when the activation deactivates after draining. Specify a step that writes a final checkpoint by calling `Stop()` while the work is held open and asserting the callback still runs. Specify one that must survive losing its activation by calling `CloseAdmission()` and asserting it handles `Closed`.

The other refusals a callback can see are `NoRun`, for a call that is not from the background work of a run, `StaleRun`, for work left over from an earlier run, and `Cancelled`, when the caller cancelled a callback that was still waiting to run.

## One run, one perform

A test run performs once. Performing it a second time throws `JobStepAlreadyHasALiveRun`. To perform again, begin a new run after the earlier one's work and callbacks have settled; beginning a run while the step's earlier run is still live also throws `JobStepAlreadyHasALiveRun`.

Beginning a run replaces the step's earlier one, so perform through the run you began last. Calling `Perform` on an earlier run throws `JobStepTestRunWasSuperseded`, and a callback from an earlier test run is refused as closed.

## What a test run does not do

A test run does not record or deliver outcomes. It does not write the step's status, call the job or count the step as finished. `Perform` returns what the step returned and nothing else happens to it. Specify those behaviors by starting and stopping the step through its grain interface and a substitute `IJob`. Starting through the grain interface copies your state for the work, so a TestKit silo that does so needs a `DeepCopier` for your state type registered.

:::caution[Specifications only]
`BeginTestRun` changes the step's run outside the activation. Never call it on a step a silo is running.
:::

Next, see [Steps](../jobs/steps.md) for how `TryInvokeOnActivation` and checkpoints behave in a real silo.
