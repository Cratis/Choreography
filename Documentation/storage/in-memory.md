---
title: In-memory storage for tests and development
description: Run Choreography without a database for tests and local development, and what you lose when the process ends.
---

In-memory storage keeps job state and patch records in the process. It needs no database, starts instantly and forgets
everything when the host stops. Use it for specifications, local development and hosts that run a single silo and never need
a job to survive a restart. Use [MongoDB](mongodb.md) or [SQL](sql.md) for anything else.

## Register it

Each kind of fact has its own registration:

```csharp
using Cratis.Choreography.Storage;

builder.Services.AddCratisChoreographyInMemoryJobsStorage();
builder.Services.AddCratisChoreographyInMemoryPatchStorage();
```

Both live in the `Cratis.Choreography.Storage` namespace of the `Cratis.Choreography.Storage` package. They register
`InMemoryJobsStorage` as `IJobsStorage` and `InMemoryPatchStorage` as `IPatchStorage`, both as singletons.

## Let the co-hosted silo do it

When you co-host the silo, `CratisChoreographyOptions.Local()` returns localhost clustering with in-memory grain storage:

```csharp
using Cratis.Choreography.Hosting;

builder.AddCratisChoreography(CratisChoreographyOptions.Local());
```

With `Storage = GrainStorageMode.InMemory` the host registers `InMemoryJobsStorage` for you, unless you registered an
`IJobsStorage` yourself. If you also turn on `Patching`, it registers `InMemoryPatchStorage` the same way, so patching works
without any storage call. Reminders are always in memory on the co-hosted silo.

## What you lose

- **Jobs do not survive a restart.** A running job disappears with the process; there is nothing to recover.
- **Every start applies every patch again.** The record of applied patches is gone, so `Up()` runs on each start. That is
  fine for tests, and it is why patches must be safe to run more than once. See [Patching](../patching/index.md).
- **Silos do not share state.** Two silos with in-memory storage each see only their own jobs and patch records, so do not
  use it for a real cluster.

:::tip[Test patches against real storage once]
In-memory storage never exercises the version gate across restarts. If a patch matters, run it once against the storage you
deploy, as described in [Apply a patch](../patching/apply-a-patch.md).
:::
