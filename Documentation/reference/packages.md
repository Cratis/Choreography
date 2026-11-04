---
title: Packages and target frameworks
description: The five Cratis Choreography NuGet packages, what each contains, which frameworks they target and what they depend on.
---

Cratis Choreography ships as five NuGet packages. Reference `Cratis.Choreography` and a storage provider for most
services, and add `Cratis.Choreography.Chronicle` only when you use Cratis Chronicle.

## Packages at a glance

| Package | Contains | Target frameworks |
| --- | --- | --- |
| `Cratis.Choreography` | Jobs, workers, state machines, workflows, patching, tenancy, typed grain keys, serializers and silo hosting | `net8.0`, `net9.0`, `net10.0` |
| `Cratis.Choreography.Storage` | The jobs and patch storage APIs and the in-memory providers | `net8.0`, `net9.0`, `net10.0` |
| `Cratis.Choreography.Storage.MongoDB` | The MongoDB providers for jobs and patches | `net8.0`, `net9.0`, `net10.0` |
| `Cratis.Choreography.Storage.Sql` | The SQL providers for jobs and patches, on EF Core for SQL Server, PostgreSQL and SQLite | `net10.0` |
| `Cratis.Choreography.Chronicle` | Event-sourced state machines and workflows whose runs are kept as Chronicle events | `net8.0`, `net9.0`, `net10.0` |

The SQL provider targets `net10.0` only, because EF Core 10 ships for `net10.0` only. The other packages are packed
for all three frameworks. A source build without packaging targets `net10.0`.

## Which packages you need

| You want | Reference |
| --- | --- |
| Jobs, workflows or state machines with no database, for development and tests | `Cratis.Choreography` |
| Durable state in MongoDB | `Cratis.Choreography` and `Cratis.Choreography.Storage.MongoDB` |
| Durable state in SQL Server, PostgreSQL or SQLite | `Cratis.Choreography` and `Cratis.Choreography.Storage.Sql` |
| Event-sourced state machines or a Chronicle-backed workflow journal | `Cratis.Choreography`, a storage provider if you run jobs, and `Cratis.Choreography.Chronicle` |

`Cratis.Choreography` references `Cratis.Choreography.Storage`, so you do not add it yourself. The storage provider
packages and the Chronicle package reference `Cratis.Choreography`.

## Dependencies

| Package | Notable dependencies |
| --- | --- |
| `Cratis.Choreography` | Microsoft Orleans 10 (`Sdk`, `Server`, `Reminders`, `Serialization` and `Serialization.SystemTextJson`), `Orleans.Providers.MongoDB`, `Cratis.Fundamentals`, `OneOf`, `System.Reactive` and the ASP.NET Core shared framework |
| `Cratis.Choreography.Storage` | `Microsoft.Orleans.Core.Abstractions`, `Cratis.Fundamentals`, `OneOf`, `System.Reactive` |
| `Cratis.Choreography.Storage.MongoDB` | `MongoDB.Driver`, `Cratis.Arc.MongoDB`, `System.Reactive` |
| `Cratis.Choreography.Storage.Sql` | `Cratis.Arc.EntityFrameworkCore` and the EF Core SQL Server, PostgreSQL and SQLite providers |
| `Cratis.Choreography.Chronicle` | `Microsoft.Orleans.Sdk`, `Cratis.Chronicle` |

Only `Cratis.Choreography.Chronicle` depends on Cratis Chronicle. The other four packages work without it.

## One Orleans version

`Orleans.Providers.MongoDB` is built for Orleans 9 and asks for the 9.x reminders package, while the rest of the stack
runs on Orleans 10. A 9.x `Microsoft.Orleans.Reminders` against a 10.x runtime fails with a `MissingMethodException` the
moment a silo builds its grain services. `Cratis.Choreography` therefore carries a build target that reads the
resolved package graph and fails your Release build when `Microsoft.Orleans.Reminders` and `Microsoft.Orleans.Server`
resolve to different versions. The package's own release build also restores a consumer for each framework and checks
that every `Microsoft.Orleans.*` package resolves to one version.

If that error appears in your build, pin `Microsoft.Orleans.Reminders` in `Directory.Packages.props`, keep its direct
`PackageReference`, and make it resolve to the same version as `Microsoft.Orleans.Server`.
See [Hosting](../hosting/index.md) for how the co-hosted silo avoids the MongoDB reminder table.

## Where to go next

- [Run your first durable job](../getting-started/index.md) uses `Cratis.Choreography` and the MongoDB provider.
- [Storage](../storage/index.md) compares the providers.
- [Options](options.md) lists every option the packages read.
