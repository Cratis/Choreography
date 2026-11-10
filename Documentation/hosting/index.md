---
title: Host the Choreography silo
description: Co-host an Orleans silo with Cratis Choreography in your ASP.NET Core app, choose clustering and storage, and see what the silo logs and traces.
---

A service that starts jobs needs an Orleans silo to run them. Cratis Choreography co-hosts that silo inside your ASP.NET Core
application with one call, so you do not build an Orleans host and then wire the job system into it yourself.

## Prerequisites

- The .NET SDK that matches the target framework of your application. The packages target .NET 10 and 11.
- A MongoDB server, if you use MongoDB clustering, grain storage or jobs storage.
- A jobs storage registration, unless you host with in-memory storage. See [Storage](../storage/index.md).

## Host the silo

```csharp
using Cratis.Choreography.Hosting;
using Cratis.Choreography.Setup;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.AddCratisChoreography(new CratisChoreographyOptions
{
    ClusterId = "my-service",
    ServiceId = "my-service"
});

builder.Services.AddCratisChoreographyMongoDBJobsStorage(
    new MongoClient("mongodb://localhost:27017"));

var app = builder.Build();
app.Run();
```

`AddCratisChoreography` registers the serializers for Cratis types, the job, job step and jobs manager grains, their grain
storage providers, jobs recovery, the Orleans cluster identity and clustering. The storage call supplies the `IJobsStorage`
those providers read and write through; `AddCratisChoreographyMongoDBJobsStorage` lives in `Cratis.Choreography.Setup`, which is
why the sample imports it. Every option is listed in [Options](../reference/options.md).

The jobs storage is resolved when the first job grain activates, not when the silo starts. A host that forgets it starts
cleanly and fails on the first job.

## Cluster on MongoDB

Out of the box the silo uses localhost clustering: one instance and no membership table. To run more than one instance, switch
to durable MongoDB membership:

```csharp
builder.AddCratisChoreography(new CratisChoreographyOptions
{
    ClusterId = "my-service",
    ServiceId = "my-service",
    Clustering = ClusteringMode.MongoDB,
    DatabaseName = "my-service-orleans",
    ConnectionString = builder.Configuration["Cratis:MongoDB:Server"]!
});
```

## Run without a database

A silo for development, tests or a runtime that hosts one silo does not need MongoDB. Keep grain state in memory and cluster on
localhost:

```csharp
builder.AddCratisChoreography(CratisChoreographyOptions.Local());
```

With `Storage = GrainStorageMode.InMemory`, jobs, job steps and, with `Workflows` on, workflow runs live in the silo's memory,
and nothing survives the process. The in-memory jobs storage is registered for you, so do not also call
`AddCratisChoreographyMongoDBJobsStorage`. With `Patching` on, the record of applied patches is kept in memory too.

## Turn on workflows and patching

Both are off by default. Each flag takes an optional callback that configures its options:

```csharp
builder.AddCratisChoreography(new CratisChoreographyOptions
{
    Workflows = true,
    ConfigureWorkflows = options => options.Scopes.Add("tenant-1"),
    Patching = true,
    ConfigurePatching = options => options.MaxAttempts = 8
});
```

`Workflows` hosts the [workflow engine](../workflows/host-and-start.md) and registers the `workflows` grain storage provider.
`Patching` applies [patches](../patching/index.md) once per store when the silo becomes active. Unless you host with in-memory
storage, register a patch storage as well; starting a silo with `Patching` on and no `IPatchStorage` fails when the patch
manager grain activates. See [Choose patch storage](../patching/choose-patch-storage.md).

## Tune the job system

`JobsOptions` properties are init-only, so assigning inside `Configure` does not compile. Register a constructed instance in the
service collection instead; `AddCratisChoreography` adds its defaults only when no `IOptions<JobsOptions>` is present, and your
registration wins whether it comes before or after the call:

```csharp
using Cratis.Choreography.Jobs;
using Microsoft.Extensions.Options;

builder.Services.AddSingleton(Options.Create(new JobsOptions
{
    MaxParallelSteps = 4,
    StepLeaseTimeout = TimeSpan.FromMinutes(2)
}));
```

## Place controllers and workers, and carry tenants

Dedicating silos to controlling jobs or performing steps is covered in [Placement](../jobs/placement.md). Carrying your tenant
context into grain calls is covered in [Tenancy](../tenancy.md). Both are opt-in registrations on the silo builder.

## Disable the silo

Pass `Enabled = false`, or bind it to configuration, and `AddCratisChoreography` returns without configuring anything. A spec
host can keep the registrations in its application and run without a silo.

## Activity propagation

`AddCratisChoreography` calls `AddActivityPropagation()` for you, so trace context follows grain calls. If your host already
called it, the filters Choreography would add are skipped. Call it **before** `AddCratisChoreography` if you want it explicit:
called afterwards, Orleans registers the incoming and outgoing propagation filters a second time and every grain call produces
duplicate spans. That is how Microsoft.Orleans.Server 10.4.2, the version this package builds against, registers them. The tenant
travels in the Orleans `RequestContext`, not in activity baggage.

## Observability

- **Logs.** Choreography loggers use categories that start with `Cratis.Choreography`. Raise that prefix to `Information` to see
  job lifecycle decisions such as drains, retries and recovery passes; `Debug` adds each status change. Around their main
  operations, job and jobs manager grains open a log scope with `Scope` and `Namespace` (jobs add `JobId`), and step grains one
  with `JobId`, `JobStepId`, `JobStepName` and `JobStepStatus`. Filter on those properties to follow one job.
- **Warnings worth alerting on.** `Job watchdog failed`, `Job failed to persist new job status`, `Jobs recovery is incomplete`,
  `Failed to recover jobs for ...`, `Retrying jobs recovery ...` and `Transient storage error during ...`. The causes and fixes
  are in [Troubleshooting](../reference/troubleshooting.md).
- **Queries.** `IJobsManager` answers `GetJobs(JobQuery)`, `GetJobsOfType<TJob, TRequest>()` and `GetAllJobs()`. Get the manager
  for a scope and namespace with `grainFactory.GetJobsManager(scope, @namespace)`; `JobQuery` filters by type, status and
  creation time and pages with `Skip` and `Take` (100 by default). `JobState.Progress` carries the step counts.

```csharp
using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;

var manager = grainFactory.GetJobsManager("my-service", "tenant-1");
var running = await manager.GetJobs(new JobQuery
{
    Statuses = [JobStatus.Running],
    Take = 20
});
```

## Orleans package alignment

Every project that references `Cratis.Choreography` gets a Release-build check that fails when `Microsoft.Orleans.Reminders`
resolves to a different version than `Microsoft.Orleans.Server`. A mismatch builds in Debug and throws `MissingMethodException`
when the silo builds its grain services. Pin the Orleans packages to one version in `Directory.Packages.props`.

## Next steps

- [Reminders and recovery](reminders-and-recovery.md) explains why reminders stay in memory and what recovery does on startup.
- [Configure Orleans yourself](configure-orleans-yourself.md) shows the registrations when you build the silo without the helper.
- [Hosting API](../reference/hosting-api.md) lists every extension method.
