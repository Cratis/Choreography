---
title: Workers
description: The background-task pattern job steps run through - and a building block of its own.
---

# Workers

A **worker** runs work off an Orleans grain's turn, so the grain can answer status and result queries while
the task runs. Use it when you need that execution boundary without a job's plan, step tracking and recovery
machinery.

The task, status and result belong to the current activation. They are not saved as job state or restored when
the grain activates again. If the work must survive a process restart, use a [job](jobs.md) with its storage
configured; running a background task alone does not provide that durability.

## The contract

Derive `GrainWithBackgroundTask<TWork, TResult>` and implement `PerformWork()`. This is a partial excerpt:
`DoAPass()` is an application-owned helper returning `Task`, not a method supplied by the base.

```csharp
using Cratis.Orleans.Workers;

public interface IEternalWatch : IGrainWithGuidKey;

public class EternalWatch : GrainWithBackgroundTask<object, string>, IEternalWatch
{
    protected override async Task<PerformWorkResult<string>> PerformWork()
    {
        await DoAPass();
        return new() { Result = "done" };
    }
}
```

A derived grain starts work through the protected `Start(token)` method; expose an application-specific grain
method if callers need to request it. That protected method is not a public grain operation. Callers poll
`GetWorkStatus`, `GetException` and `GetResult`; `GetResult` reads the currently available result rather than
waiting for the task or consuming it.

## What it guarantees

| Guarantee | Meaning |
| --- | --- |
| Off-turn execution | The grain can answer calls while the background task runs off its activation scheduler |
| Status and result queries | `GetWorkStatus`, `GetException` and `GetResult` expose the activation's current values |

The base does not automatically cancel work on deactivation, provide throttle slots, or persist its status and
result. Cancellation, concurrency limits and durable progress need an application design or the jobs machinery;
they are not guarantees of the worker base.

## Where the job system uses it

Every job step grain derives `JobStep<TRequest, TResult, TState>`, which derives a worker: the step's `PerformStep` runs as the background task, so a slow step never holds the job grain's bookkeeping turn, and stopping a job cancels the token the step observes. See [the job system](jobs.md).
