---
title: Workers for background grain work
description: Run long or CPU-bound work in the background of an Orleans grain, then poll its status and fetch its result, with the pattern job steps run on.
---

An Orleans grain handles one call at a time. A grain that scans a large inventory, hashes a file or crunches numbers for
thirty seconds holds its turn for all of that time, so every other call to it waits and the caller's request times out. A
**worker** is a grain that starts such work in the background and answers immediately. Callers then poll its status and fetch
the result when it is done. The job step grains run on this pattern, and you can use it directly for any grain that needs the same shape.

## The contract

Derive from `GrainWithBackgroundTask<TRequest, TResult>`, implement `PerformWork()` and call `Start()` from one of your own grain
methods. Expose the grain through an interface that extends `IGrainWithBackgroundTask<TRequest, TResult>`, which is a grain
interface keyed by a `Guid`:

```csharp
using Cratis.Choreography.Workers;

[GenerateSerializer]
public record ScanRequest(int Items);

public interface IInventoryScan : IGrainWithBackgroundTask<ScanRequest, int>
{
    Task<bool> Begin(ScanRequest request);
}

public class InventoryScan : GrainWithBackgroundTask<ScanRequest, int>, IInventoryScan
{
    ScanRequest _request = new(0);

    public Task<bool> Begin(ScanRequest request)
    {
        _request = request;
        return Start();
    }

    protected override async Task<PerformWorkResult<int>> PerformWork()
    {
        var scanned = 0;
        for (var i = 0; i < _request.Items; i++)
        {
            await ScanOne(i);
            scanned++;
        }

        return new() { Result = scanned };
    }
}
```

`ScanOne` stands for your own work. `TRequest` only types the grain; the base class does not hand it to `PerformWork`, so the grain
keeps the request itself, as `Begin` does here. A caller starts the work, polls and reads the result:

```csharp
var scan = grains.GetGrain<IInventoryScan>(Guid.NewGuid());

var started = await scan.Begin(new(5));          // true
var again = await scan.Begin(new(5));            // false: the work is already started
var status = await scan.GetWorkStatus();         // Running, then Completed
var result = await scan.GetResult();
if (result.TryGetResult(out var work))
{
    Console.WriteLine(work.Result);              // 5
}
```

## What the worker does for you

| Behavior | Meaning |
| --- | --- |
| Off-turn execution | `Start()` queues `PerformWork` on the thread pool and returns `true` at once, so the grain keeps answering calls |
| One run at a time | A second `Start()` while the work runs, or after it completed, returns `false`. After a failure or a cancellation the grain can start again |
| Status | `GetWorkStatus()` answers `NotStarted`, `Running`, `Completed`, `Failed` or `Stopped` |
| Result | `GetResult()` never waits. It answers the `PerformWorkResult<TResult>`, or a `WorkerGetResultError`: `NotStarted`, `NotFinished` or `WorkCancelled` |
| Errors | An exception from `PerformWork` is caught: the status becomes `Failed`, `GetException()` returns it, and the result carries it with `PerformWorkError.WorkerError` |
| Cancellation | If the token passed to `Start()` is already cancelled when the work would begin, the status becomes `Stopped` and the result `WorkCancelled`. A work that returns `PerformWorkError.Cancelled` ends the same way |

A `PerformWorkResult<TResult>` holds the `Result`, an `Error` (`None`, `Cancelled`, `WorkerError` or `PerformingWorkError`) and an
optional `Exception`; `IsSuccess` is true when there is neither error nor exception.

## What it leaves to you

- **No durability.** The work lives in memory. If the grain deactivates, or its silo goes away, the work is gone. Job steps carry Orleans'
  `[KeepAlive]` attribute so idle collection does not deactivate them; a worker that must not be deactivated mid-work needs the same, and one that must survive losing its silo belongs in a job.
- **No cancellation plumbing.** `PerformWork()` takes no token, and the token given to `Start()` is looked at only before the work
  begins. Hold your own `CancellationTokenSource` and cancel it from a grain method or from `OnDeactivateAsync`.
- **No concurrency cap.** Each grain runs one piece of work. Limit how many grains work at once yourself, as the job engine does with
  its step throttle.

## Where the job system uses it

Every job step grain derives `JobStep<TRequest, TResult, TState>`, which derives a worker. The step's `PerformStep` runs as the
background task, so a slow step never holds the job grain's bookkeeping turn, and stopping a job cancels the token the step
observes. That step adds the durability, retries and recovery a bare worker lacks; see [the job system](jobs/index.md).

## When a worker is the wrong fit

Reach for a worker when you need one in-memory piece of background work and can afford to lose it with the grain. When the work must
survive a restart, retry, run in stages or report progress, define a [job](jobs/index.md). When the work is a business process with
waits and branches, use a [workflow](workflows/index.md).
