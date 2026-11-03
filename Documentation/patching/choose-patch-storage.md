---
title: Choose patch storage
description: Pick and register the IPatchStorage that holds applied patches, from in-memory to MongoDB or SQL, and see what each one writes.
---

The patch manager is only as durable as the record of what it has applied. Patching needs an `IPatchStorage`, and starting
without one throws when the silo starts. That is intentional: patching against a store that cannot remember what ran would
apply every patch on every start.

The record is **per store**, not per scope or namespace. A patch changes the store itself, so every silo in the cluster must
read and write the same record. This differs from jobs storage; see [Storage](../storage/index.md). Pick the storage you
deploy, not the one that is convenient locally, and register it next to your jobs storage.

| Storage | Survives a restart | Use it for |
| --- | --- | --- |
| In-memory | No | Tests and local development |
| MongoDB | Yes | Hosts that run on MongoDB |
| SQL | Yes | Hosts that run on SQL Server, PostgreSQL or SQLite |

## In-memory

```csharp
using Cratis.Choreography.Storage;

builder.Services.AddCratisChoreographyInMemoryPatchStorage();
```

With `CratisChoreographyOptions.Patching` and `Storage = GrainStorageMode.InMemory`, the co-hosted silo registers it for you.
Nothing survives the process, so every start applies every patch again.

## MongoDB

```csharp
using Cratis.Choreography.Setup;
using MongoDB.Driver;

builder.Services.AddCratisChoreographyMongoDBPatchStorage(
    new MongoClient("mongodb://localhost:27017"),
    options => options.DatabaseName = "choreography");
```

Records go to one database, `choreography` unless you change `MongoDBPatchStorageOptions.DatabaseName`:

| Collection | Document |
| --- | --- |
| `patches` | One per applied patch, keyed by patch name, with `version` and `appliedAt` |
| `patch-versions` | A `patched` document and a `host` document, each with a `version` |

Saving a patch that is already recorded replaces its document. A client registered earlier, for example by the jobs storage,
is kept. More in [MongoDB storage](../storage/mongodb.md).

## SQL

```csharp
using Cratis.Choreography.Setup;
using Cratis.Choreography.Storage.Sql.Patching;
using Microsoft.EntityFrameworkCore;

builder.Services.AddCratisChoreographySqlPatchStorage(options =>
    options.OptionsResolver = () =>
        new DbContextOptionsBuilder<PatchesDbContext>()
            .UseNpgsql("Host=localhost;Database=choreography;Username=app;Password=<password>")
            .Options);
```

The resolver takes no arguments: unlike jobs storage it is not asked about a scope or namespace. The tables are `Patches`
(`Name`, `Version`, `AppliedAt`) and `PatchVersions` (`Id` of `patched` or `host`, and `Version`). You do not create them;
`SqlPatchStorage` applies its migration the first time it is used. More in [SQL storage](../storage/sql.md).

## Why two versions are stored

`IPatchStorage` keeps the applied patches plus two versions: the patched version (the newest patch applied) and the host
version (the newest host with a known version that patched the store). The first stops a patch from running twice, the
second stops an older host from patching a newer store. The [overview](index.md) explains how the manager uses them. If you
write your own `IPatchStorage`, see the [reference](reference.md) for the members.

:::caution[Use one patch store per Choreography store]
Two clusters that share a store but record patches in different places each believe nothing has run. Point every silo that
shares jobs and workflow state at the same patch storage.
:::
