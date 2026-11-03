---
title: Storage in Cratis Choreography
description: How Choreography finds the database for job state and patch records, which providers exist, and what is scoped versus store-wide.
---

Choreography keeps two kinds of durable facts, and they find their database in different ways. Mixing them up is the usual
source of "my jobs are in one database and my patches in another" surprises, so the split comes first.

| Facts | Resolved by | Scoped by | Providers |
| --- | --- | --- | --- |
| Job and job step state | `IJobsStorage` | scope and namespace | [MongoDB](mongodb.md), [SQL](sql.md), [in-memory](in-memory.md) |
| Which patches have run and how far | `IPatchStorage` | nothing - one set per store | [MongoDB](mongodb.md), [SQL](sql.md), [in-memory](in-memory.md) |

You register one provider for each. Registering jobs storage does not register patch storage.

## Jobs storage is resolved per scope and namespace

Every storage touch of the job system - reading a job, saving a step, counting progress - goes through one seam:

```csharp
JobsStorage GetFor(string scope, string @namespace);
```

The engine never decides where job state lives. The two values are opaque to it: it takes them from the job grain's key and
hands them back unchanged. A provider decides what the pair resolves to:

- **MongoDB** maps the pair to a database name, `{scope}+jobs` for an empty namespace and `{scope}+jobs+{namespace}`
  otherwise. You can replace that rule with a resolver.
- **SQL** asks your resolver for the EF Core `DbContextOptions` of the pair, so you choose the database provider and
  connection per scope and namespace.

The pair travels inside the grain key, so it survives everything the grain survives. A job activated on another silo lands in
the same database as the silo that started it.

## What the pair means is yours

| Consumer | Scope | Namespace |
| --- | --- | --- |
| Cratis Chronicle's kernel | event store name | event store namespace |
| An application | the service, the tenant, or anything storage should hang off | a subdivision of it, or empty |

Because the choice is open, keep the values stable. Changing what a scope maps to moves jobs out of sight: the database name
you compute for the new value does not exist, and reads come back empty rather than failing.

## Patch storage is per store

A patch changes the data the whole store holds, so every silo in the cluster must read and write the same record of what has
been applied. `IPatchStorage` therefore has no scope or namespace: the MongoDB provider uses one database for the whole store,
and the SQL provider uses one `PatchesDbContext`. See [Choose patch storage](../patching/choose-patch-storage.md) for the
choice and [Patching](../patching/index.md) for what the records are used for.

## Resetting what was resolved

`IJobsStorage.Reset()` forgets everything resolved so far. Resolution is cached, and for SQL part of resolving is applying
migrations once per scope and namespace. If you empty or recreate the database behind the job system, call `Reset()`
afterward. Nothing fails at that moment; without it, the next write reports a table that is not there.

## Where to go next

- [Storage names](../reference/storage-names.md) lists every collection, table and grain storage provider name.
- [Host the silo](../hosting/index.md) shows how the co-hosted silo picks up jobs storage.
