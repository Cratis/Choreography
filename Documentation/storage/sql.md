---
title: SQL storage for jobs and patches
description: Register SQL Server, PostgreSQL or SQLite for job state and patch records through EF Core, with migrations applied for you.
---

The SQL provider ships in the `Cratis.Choreography.Storage.Sql` package and uses EF Core. It is provider-agnostic: your
resolver returns `DbContextOptions`, so you choose SQL Server, PostgreSQL or SQLite and the connection. The package targets
`net10.0` when packaged, because EF Core 10 ships only for it.

## Register jobs storage

The jobs resolver receives the scope and namespace, so you can map each pair to its own database:

```csharp
using Cratis.Choreography.Setup;
using Cratis.Choreography.Storage.Sql;
using Microsoft.EntityFrameworkCore;

builder.Services.AddCratisChoreographySqlJobsStorage(options =>
    options.OptionsResolver = (scope, @namespace) =>
        new DbContextOptionsBuilder<JobsDbContext>()
            .UseSqlServer($"Server=.;Database={scope}_jobs;Trusted_Connection=True;TrustServerCertificate=true")
            .Options);
```

`SqlJobsStorageOptions.OptionsResolver` is required and has the type
`Func<string, string, DbContextOptions<JobsDbContext>>`. The configure callback is not optional, because the storage cannot
work without a resolver.

## Register patch storage

The patch resolver takes **no arguments**, because patch records are per store and not per scope or namespace:

```csharp
using Cratis.Choreography.Setup;
using Cratis.Choreography.Storage.Sql.Patching;
using Microsoft.EntityFrameworkCore;

builder.Services.AddCratisChoreographySqlPatchStorage(options =>
    options.OptionsResolver = () =>
        new DbContextOptionsBuilder<PatchesDbContext>()
            .UseSqlServer("Server=.;Database=choreography;Trusted_Connection=True;TrustServerCertificate=true")
            .Options);
```

`SqlPatchStorageOptions.OptionsResolver` is required and has the type `Func<DbContextOptions<PatchesDbContext>>`. Copying the
jobs resolver and keeping its `(scope, @namespace)` parameters does not compile.

## Migrations run for you

You do not create the schema. Both storages call `Database.Migrate()` themselves:

- `SqlJobsStorage` migrates the first time it resolves a scope and namespace, once per pair, because `GetFor` caches what it
  builds. A scope that maps to a database nothing has provisioned yet still works, as long as the connection can create it.
- `SqlPatchStorage` migrates the first time any of its members is used.

In both cases concurrent callers are serialized so two of them cannot race to create the same tables. Do not call
`EnsureCreated` on these contexts: it creates the tables outside the migration history, and the migration then fails on tables
that already exist.

:::note[After emptying the database, reset]
Migrations run once per cached resolution. If you empty or recreate a database behind the job system, call
`IJobsStorage.Reset()` so the next resolution migrates again. See [Storage](index.md).
:::

## Tables

| Table | Context | Holds |
| --- | --- | --- |
| `Jobs` | `JobsDbContext` | Jobs, with indexed `Status` and `Type` columns and a `StateJson` column |
| `JobSteps` | `JobsDbContext` | Job steps, including failed ones, told apart by `Status` |
| `Patches` | `PatchesDbContext` | One row per applied patch: `Name` (key, up to 256 characters), `Version`, `AppliedAt` |
| `PatchVersions` | `PatchesDbContext` | A `patched` row and a `host` row, each with a `Version` |

State is stored as JSON in `StateJson`, using the provider's native JSON type. On PostgreSQL the column is `jsonb`, and the
context binds it explicitly so writes do not fail on the implicit cast from text. Unlike MongoDB, failed steps are not moved
to their own table. See [Storage names](../reference/storage-names.md).

## Jobs serializer options

`SqlJobsStorage` resolves `JsonSerializerOptions` from the host's services when you registered one, and uses a default
instance otherwise. Register yours if job state needs custom converters.
