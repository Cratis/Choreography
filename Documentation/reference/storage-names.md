---
title: Storage names reference
description: Every MongoDB collection, SQL table, database name and grain storage provider name that Choreography reads and writes.
---

These names are fixed by the library. They matter when you query the data yourself, back it up, grant database permissions, or
keep a dashboard on it. Each name is a constant you can reference in code.

## MongoDB collections

`WellKnownCollectionNames`, in `Cratis.Choreography.Storage.MongoDB`:

| Constant | Collection | Lives in | Holds |
| --- | --- | --- | --- |
| `Jobs` | `jobs` | The job database of a scope and namespace | Job state |
| `JobSteps` | `job-steps` | The job database | Job step state |
| `FailedJobSteps` | `failed-job-steps` | The job database | Failed job steps |
| `JobStepPlans` | `job-step-plans` | The job database | Step plans too large to live inside the job document |
| `Patches` | `patches` | The patch database | One document per applied patch |
| `PatchVersions` | `patch-versions` | The patch database | The `patched` and `host` version documents |

## MongoDB databases

| Facts | Database | Changed by |
| --- | --- | --- |
| Jobs, empty namespace | `{scope}+jobs` | `MongoDBJobsStorageOptions.DatabaseNameResolver` |
| Jobs, with a namespace | `{scope}+jobs+{namespace}` | `MongoDBJobsStorageOptions.DatabaseNameResolver` |
| Patches | `choreography` | `MongoDBPatchStorageOptions.DatabaseName` |
| Grain state and cluster membership of the co-hosted silo | `orleans` | `CratisChoreographyOptions.DatabaseName` |

`DatabaseNames.ForJobs` throws `InvalidDatabaseName` for a name that is empty, longer than 63 UTF-8 bytes, or contains one of
`/ \ . space " $ * < > : | ?` or a null character.

## SQL tables

`WellKnownTableNames`, in `Cratis.Choreography.Storage.Sql`:

| Constant | Table | Context | Holds |
| --- | --- | --- | --- |
| `Jobs` | `Jobs` | `JobsDbContext` | Job state |
| `JobSteps` | `JobSteps` | `JobsDbContext` | Job steps, failed ones included, told apart by `Status` |
| `Patches` | `Patches` | `PatchesDbContext` | One row per applied patch |
| `PatchVersions` | `PatchVersions` | `PatchesDbContext` | The `patched` and `host` version rows |

## Grain storage provider names

`WellKnownGrainStorageProviders`, in `Cratis.Choreography`. These are the names the grains ask Orleans for, and the names to
use when you configure Orleans yourself:

| Constant | Name | Used by |
| --- | --- | --- |
| `Jobs` | `jobs` | Job grains |
| `JobSteps` | `job-steps` | Job step grains |
| `Workflows` | `workflows` | The facts of workflow runs |

## Grain type name

The patch manager grain is registered as `cratis-choreography-patch-manager`, with an integer key of `0`. It appears in the
Orleans placement failure that `IPatchRunner` waits out while the cluster's manifests converge.

See also [Storage](../storage/index.md) and [Choose patch storage](../patching/choose-patch-storage.md).
