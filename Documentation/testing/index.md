---
title: Testing code built on Choreography
description: Choose between a single in-memory silo, Orleans TestKit unit specs and a real cluster when you test jobs, patches and state machines built on Choreography.
---

Durable processes fail in ways a unit test never sees: a step runs twice after a silo dies, a stop arrives while a callback is mid-write, a patch applies on a store a newer release already marked. Testing them is a matter of picking the cheapest setup that still exercises the behavior you care about.

Choreography's own specs use three levels. Each is available to you, and each has a different cost.

| Level | What runs | Use it for |
| --- | --- | --- |
| Orleans TestKit | One grain, hosted in a fake silo with substituted storage | A step's own work, a state machine's transitions |
| In-memory silo | A real silo in your test process, nothing persisted | Starting a job or workflow end to end, patch application |
| Cluster against a database | Real silos, real MongoDB or SQL storage | Recovery, draining and anything that depends on stored state |

Start at the top of the table and move down only when a lower level cannot show the behavior.

## Test a grain with Orleans TestKit

The framework's `*.Specs` projects reference the `OrleansTestKit10` package, which hosts a grain without a silo. You register what the grain would receive from the silo, such as persistent state storage and services, and create the grain:

```csharp
var silo = new TestKitSilo();
silo.AddService(Options.Create(new JobsOptions()));
var step = await silo.CreateGrainAsync<MyStep>(stepId, keyExtension: key);
```

This excerpt shows the shape, not a complete spec: each grain needs its own storage and services. A job step additionally needs a test run, because a step's work only reaches its grain through a run. [Test job steps](job-steps.md) covers that.

## Run a silo in memory

`CratisChoreographyOptions.Local()` returns localhost clustering and in-memory grain storage: jobs, job steps and workflow runs live in the memory of one silo and nothing survives the process. It needs no database, so it suits specs that start a job and watch it finish.

```csharp
var builder = WebApplication.CreateBuilder();
builder.Configuration["urls"] = "http://127.0.0.1:0";
builder.AddCratisChoreography(CratisChoreographyOptions.Local());

await using var app = builder.Build();
await app.StartAsync();

var jobs = app.Services.GetRequiredService<IGrainFactory>().GetJobsManager("tests", string.Empty);
```

The silo has finished starting when `StartAsync` returns, so the grain factory is ready. Binding the HTTP address to port `0` picks a free port, because the silo is only reached in process.

`Local()` does not turn on workflows or patching. Those are properties of `CratisChoreographyOptions`, which has `init`-only properties, so build the options with an object initializer instead:

```csharp
builder.AddCratisChoreography(new CratisChoreographyOptions
{
    Clustering = ClusteringMode.Localhost,
    Storage = GrainStorageMode.InMemory,
    Workflows = true,
    Patching = true
});
```

:::caution[One localhost silo per machine]
Localhost clustering uses the default Orleans ports (the silo logs the address `127.0.0.1:11111`), so two such silos cannot run at once. Share one silo across the specs of a test run, as the integration fixtures do, rather than starting one per class.
:::

Jobs run in the background, so a spec waits for a job to reach a terminal status by polling `GetAllJobs()` under a deadline, not by sleeping for a fixed time. See [Manage jobs](../jobs/manage-jobs.md) for the queries that report job status.

## Test against a real cluster

The `Integration/` folder of the repository holds two projects that show the patterns.

- `Jobs.Integration` starts a silo through the public `AddCratisChoreography` surface and registers MongoDB jobs storage against `mongodb://localhost:27017`, with a second fixture that uses SQL storage (SQLite by default; set `CRATIS_CHOREOGRAPHY_SQL_POSTGRES` to a connection string to use PostgreSQL). Its fixture exposes one shared silo and a reset method, and its specs sit in a collection with parallelization disabled, because every spec resets the same database.
- `Chronicle.Integration` uses an Orleans `TestCluster` of two silos, so a grain call can cross silos, with in-memory read models instead of a Chronicle server.

Run `Jobs.Integration` with a MongoDB instance reachable on that address. Reuse the shared fixture, reset state at the start of each spec, and give the cluster its own cluster id, service id and database name so a run cannot disturb another.

## Test a patch

A patch is a class that implements `ICanApplyPatch`. Test its `Up()` directly for what it does to your data. To test that it is discovered, ordered and recorded, start a silo with `Patching = true` and a `HostVersion`, as the `for_Patching` specs do:

```csharp
builder.AddCratisChoreography(new CratisChoreographyOptions
{
    Clustering = ClusteringMode.Localhost,
    Storage = GrainStorageMode.InMemory,
    Patching = true,
    ConfigurePatching = options => options.HostVersion = new SemanticVersion(2, 0, 0)
});
builder.Services.AddSingleton<PatchProbe>();
```

`PatchProbe` is the spec's own singleton that each patch records itself in. The patches are applied before startup completes, so after `StartAsync` the spec reads the probe and asserts that patches ran oldest first, and reads `IPatchStorage` to assert what was recorded: the applied patches, the patched version (`GetPatchedVersion()`) and the host version (`GetHostVersion()`). With `GrainStorageMode.InMemory` the record of applied patches is in memory too. See [Patching](../patching/index.md) for how versions decide who applies a patch.

## Test a state machine

A state reaches its machine through `StateMachine`. To test a state without a grain, give it a substitute machine:

```csharp
var state = new Draft();
var stateMachine = Substitute.For<IStateMachine<DocumentState>>();
state.SetStateMachine(stateMachine);
```

This excerpt assumes your own `Draft` state and `DocumentState` stored state. To test the machine itself, host it in a TestKit silo with substituted state storage, as the framework's `for_StateMachine` specs do. For an event-sourced machine, `Chronicle.Integration` shows delivery across a two-silo cluster. [State machines](../state-machines/persistence-and-testing.md) explains persistence and activation.

## When not to go further down

Each step down the table costs seconds and an external dependency. Prefer TestKit for logic a single grain owns, an in-memory silo for behavior across grains, and a database-backed cluster only for what depends on stored state or on a silo disappearing.
