---
title: Place job controllers and workers on separate silos
description: Choose which Orleans silos host job controllers and workers, write a placement director, and limit how many job steps each silo runs at once.
---

A job that fans out thousands of steps can starve the silos that serve your API: step work is the heaviest thing a silo does, and by default it lands wherever Orleans finds capacity. If you want controllers on small, stable silos and step work on large, disposable ones, you need to say where each kind of grain may activate. This page shows how, and what happens to a job when no silo qualifies.

## Where jobs run by default

A job is a grain of its own type, so Orleans only considers silos that have that job's class loaded. A silo that does not host a particular job never receives it.

Among the silos that do host it, the `Job` base class asks for resource-optimized placement: a new job activates on the silo with the most headroom for CPU, memory and activations, not on whichever silo received the request that started it. Step grains carry no placement of their own and follow the cluster's default placement. Scaling a service out therefore spreads job load without any configuration.

If every silo is interchangeable, stay with the defaults. The rest of this page is for clusters where silos differ in size or role.

## Choose a placement per kind of grain

Two independent opt-ins on the silo builder, both in `Cratis.Choreography.Hosting`. Neither is on by default.

```csharp
builder.UseOrleans(silo =>
{
    silo.AddJobPlacement<JobControllerDirector>();
    silo.AddJobStepPlacement<JobWorkerDirector>();
    silo.UseJobStepParallelismFromSiloMetadata();
});
```

| Call | What it does |
| --- | --- |
| `AddJobPlacement()` | Routes job grains through the job placement strategy but keeps resource-optimized placement, as without the call. Use it when a director is registered by another call. |
| `AddJobPlacement<TDirector>()` | Replaces the placement of every job grain with your `IPlacementDirector`. |
| `AddJobStepPlacement()` | Routes step grains through the step placement strategy but keeps the cluster's default placement. |
| `AddJobStepPlacement<TDirector>()` | Replaces the placement of every step grain with your `IPlacementDirector`. |

A director registered with the generic overload wins whatever the call order, and the director is registered as a singleton. The strategies apply to every concrete grain that derives from `Job<,>` or `JobStep<,,>`; the opt-in replaces any placement attribute on those classes, including the one `Job` declares.

Placement is a property the cluster agrees on, so register the same directors on every silo, and enable a strategy only once every silo runs a release that includes job placement (Cratis.Orleans 1.30.0 or Cratis.Choreography 2.0.0 and later).

## Write a director

A director chooses among the silos Orleans considers compatible for the grain. This one reads a `role` entry that each silo publishes with Orleans' `UseSiloMetadata`:

```csharp
using Cratis.Choreography.Jobs.Placement;
using Orleans.Runtime.MembershipService.SiloMetadata;
using Orleans.Runtime.Placement;

public sealed class JobWorkerDirector(ISiloMetadataCache metadata) : IPlacementDirector
{
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        var workers = context.GetCompatibleSilos(target)
            .Where(silo => metadata.GetSiloMetadata(silo).Metadata.TryGetValue("role", out var role) && role == "worker")
            .ToList();

        return workers.Count == 0
            ? throw new NoEligibleJobWorker(target.GrainIdentity, "no silo has the worker role")
            : Task.FromResult(workers[Random.Shared.Next(workers.Count)]);
    }
}
```

A controller director has the same shape. Each silo then publishes its role when it starts, for example `silo.UseSiloMetadata(new Dictionary<string, string> { ["role"] = "worker" })`.

:::caution[Directors run on the activation path]
A director must answer promptly and must not call grains or storage. Every activation of that kind of grain waits for it.
:::

Throw `NoEligibleJobWorker` when no silo qualifies. The framework treats that exception differently depending on when it happens.

## When no silo qualifies

| Moment | Outcome |
| --- | --- |
| Starting a job, and preparing a step throws | The job ends `Failed` with the director's message in its status history, and `Start` returns `StartJobError.Unknown`. Nothing is retried. |
| Starting a job, and starting a prepared step throws | The step counts as failed and the job completes with failures. The step's stored state stays `Prepared`. |
| Resuming or recovering a job | The job stays `Running`. Its in-memory watchdog retries the step once per `JobWatchdogInterval` until a worker appears. |

That last retry is not durable. It does not survive the job's own silo going away, and Choreography has no durable waiting state for a job that has no worker. If the only worker silo is lost, the job waits until another one joins, and recovery then picks it up. See [Job recovery](recovery.md) for what rehydration does after a silo is lost.

## Set how many steps each silo runs

`JobsOptions.MaxParallelSteps` limits how many steps run concurrently on one silo. It defaults to `Math.Max(1, Environment.ProcessorCount - 1)` and applies per silo, so a cluster of mixed machines needs per-silo values.

`UseJobStepParallelismFromSiloMetadata()` makes each silo read its limit from the silo metadata key `cratis.jobs.max-parallel-steps`. Pass another key as the argument to use a different name. A missing, unparseable or non-positive value falls back to the `JobsOptions` limit, and an invalid value is logged.

```csharp
silo.UseSiloMetadata(new Dictionary<string, string>
{
    ["role"] = "worker",
    ["cratis.jobs.max-parallel-steps"] = "16"
});
```

`UseJobStepParallelismFromSiloMetadata()` replaces the registered step throttle and works before or after `AddCratisChoreography`. The [options reference](../reference/options.md) lists every `JobsOptions` setting.

## Next steps

- [Recover jobs after a silo is lost](recovery.md) covers what happens to controllers and steps when their silo goes away.
- [Host the silo](../hosting/index.md) shows where these calls sit in a complete host.
