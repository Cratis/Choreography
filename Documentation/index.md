---
title: Cratis Choreography on Orleans
description: Cratis Choreography is the runtime for durable business processes on Microsoft Orleans, with jobs, workflows, state machines and storage that survive restarts.
---

Cratis Choreography is a .NET library for developers who build long-running behavior on Microsoft Orleans: work that
takes minutes, waits for people, or must finish even though the process that started it restarts. It provides jobs,
workflows and state machines, the storage they persist through, and the hosting that puts an Orleans silo next to your
application in one call.

Without it, every service that runs long-lived work on Orleans grows its own job engine, its own serializers and its
own knowledge of which Orleans packages can be mixed, and each copy fails in its own way after a deploy. With it,
you add a package, describe the work, and get one engine with durable state, retries, progress and recovery. The
library was consolidated from the job engines of Cratis Chronicle, Direct and Studio, and all three consume it.

## Start here

New to Choreography? Build something and watch it survive a restart:

1. [Run your first durable job](getting-started/index.md) builds a two-step job, then kills the process mid-job and
   resumes it.
2. [Run your first workflow](getting-started/first-workflow.md) models an order process as a graph of nodes.
3. [Choosing a building block](choosing-a-building-block.md) helps when you are not sure whether you need a job, a
   workflow, a state machine or a worker.

## What is in it

| Area | Use it when | Start with |
| --- | --- | --- |
| Jobs | Work must not be lost, and its steps can run in parallel and be observed | [Jobs](jobs/index.md) |
| Workflows | A business process branches, waits for people or timers, or fans out | [Workflows](workflows/index.md) |
| State machines | A grain has a lifecycle, plain or driven by Chronicle events | [State machines](state-machines/index.md) |
| Operations | Users must see what they did and undo or redo it as new, recorded operations | [Operations and undo/redo](operations/index.md) |
| Workers | One grain must run background work off its turn | [Workers](workers.md) |
| Live collaboration | People open the same page and must see each other's cursors | [Live collaboration](collaboration/index.md) |
| Hosting | You need the silo, its clustering and its options | [Hosting](hosting/index.md) |
| Storage | Job and workflow state must persist in MongoDB, SQL or memory | [Storage](storage/index.md) |
| Patching | A release must change stored data once, on start | [Patching](patching/index.md) |
| Tenancy | Grain calls must carry the caller's tenant | [Tenancy](tenancy.md) |
| Upgrading | Silos of two releases run in one cluster | [Upgrading and compatibility](upgrading/index.md) |
| Testing | You want to test jobs, state machines and patches | [Testing](testing/index.md) |

Every option, extension method and storage name is listed in the [reference](reference/options.md). For a symptom you
are looking at, start with [Troubleshooting](reference/troubleshooting.md).

## Packages

Five packages carry the library. They target `net8.0`, `net9.0` and `net10.0`, except the SQL provider, which targets
`net10.0` only.

| Package | Holds |
| --- | --- |
| `Cratis.Choreography` | Jobs, workers, state machines, workflows, typed grain keys, serializers, live collaboration presence and silo hosting |
| `Cratis.Choreography.Storage` | The jobs storage API and in-memory storage |
| `Cratis.Choreography.Storage.MongoDB` | The MongoDB storage provider |
| `Cratis.Choreography.Storage.Sql` | The SQL storage provider on EF Core for SQL Server, PostgreSQL and SQLite |
| `Cratis.Choreography.Chronicle` | Event-sourced state machines and workflows whose runs are kept as Chronicle events |

Apart from `Cratis.Choreography.Chronicle`, nothing depends on Cratis Chronicle. Chronicle is a consumer of the library,
like any other service. See [Packages](reference/packages.md) for dependencies and target frameworks.
