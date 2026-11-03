---
title: MongoDB storage for jobs and patches
description: Register MongoDB for job state and patch records, and learn the database names, collections, indexes and retry options it uses.
---

The MongoDB provider ships in the `Cratis.Choreography.Storage.MongoDB` package. It stores job state in one database per
scope and namespace, and patch records in one database for the whole store. Both registrations take an `IMongoClient` that
you create, so connection settings and pool size stay under your control.

## Register jobs storage

```csharp
using Cratis.Choreography.Setup;
using MongoDB.Driver;

builder.Services.AddCratisChoreographyMongoDBJobsStorage(
    new MongoClient("mongodb://localhost:27017"));
```

The call registers the client, `MongoDBJobsStorage` as `IJobsStorage`, the BSON serializers job state needs, and Arc's
process-wide change-stream watcher that observing jobs shares. Share one `IMongoClient` for the whole process, because each
client owns its own connection pool.

## Database names for jobs

With no resolver, `DatabaseNames.ForJobs` decides:

| Scope | Namespace | Database |
| --- | --- | --- |
| `orders` | empty | `orders+jobs` |
| `orders` | `eu` | `orders+jobs+eu` |

`DatabaseNames.ForJobs` rejects a name MongoDB cannot use: empty, longer than 63 bytes in UTF-8, or containing any of
`/ \ . space " $ * < > : | ?` or a null character. It throws `InvalidDatabaseName`, which names the scope and namespace that
produced it, instead of letting MongoDB fail later with a bare `Invalid namespace specified`. A tenant id with a space in it
is the typical cause.

To keep an existing naming scheme, pass a resolver. A resolver replaces `DatabaseNames.ForJobs` entirely, so it is also
responsible for returning legal names:

```csharp
builder.Services.AddCratisChoreographyMongoDBJobsStorage(
    new MongoClient("mongodb://localhost:27017"),
    options => options.DatabaseNameResolver =
        (scope, @namespace) => $"{scope}+es+{@namespace}");
```

## Options for jobs storage

`MongoDBJobsStorageOptions`:

| Option | Default | Meaning |
| --- | --- | --- |
| `DatabaseNameResolver` | not set | Maps scope and namespace to a database name; when not set, `DatabaseNames.ForJobs` is used |
| `TransientRetryCount` | `5` | Retries after a transient failure; `0` disables retrying |
| `TransientRetryBaseDelay` | 200 ms | Delay before the first retry; it doubles for each following retry and is randomized |

Only `MongoWaitQueueFullException`, `TimeoutException`, `MongoConnectionException` and `MongoExecutionTimeoutException` are
retried. Everything else surfaces immediately.

## Collections and indexes

Each job database holds these collections (see [Storage names](../reference/storage-names.md) for the constants):

| Collection | Holds |
| --- | --- |
| `jobs` | Job state |
| `job-steps` | Job step state |
| `failed-job-steps` | Failed job steps, moved out of `job-steps` |
| `job-step-plans` | Step plans too large to live inside the job document |

The storage ensures its indexes the first time it uses a collection: `status`, `type_status`, `status_created`,
`type_status_created` and `status_stopFinalized` on `jobs`, `jobId` on `job-steps` and `failed-job-steps`, and
`jobId_incarnation_index` on `job-step-plans`. Concepts serialize as their underlying values and Guids use the standard
representation.

## Connection pool

The library never creates the client, so these `MongoClientSettings` are yours:

- `MaxConnectionPoolSize` (driver default 100) caps concurrent operations; more wait in a queue.
- `MaxConnecting` (driver default 2) limits how fast a cold pool opens connections, so a burst at startup queues. Raise it if
  startup shows `MongoWaitQueueFullException`.

## Register patch storage

```csharp
using Cratis.Choreography.Setup;
using MongoDB.Driver;

builder.Services.AddCratisChoreographyMongoDBPatchStorage(
    new MongoClient("mongodb://localhost:27017"),
    options => options.DatabaseName = "choreography");
```

The `configure` callback is optional. `MongoDBPatchStorageOptions.DatabaseName` defaults to `choreography`. Patch records live
in that single database, in the `patches` collection (one document per applied patch, keyed by name) and the `patch-versions`
collection (a `patched` document and a `host` document). If a client is already registered, for example by the jobs storage,
the existing one is kept and the one you pass here is ignored. See [Choose patch storage](../patching/choose-patch-storage.md).
