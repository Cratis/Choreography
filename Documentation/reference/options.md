---
title: Choreography options reference
description: Every Choreography options class with its property names, defaults and meaning, from the silo host to jobs, workflows, patching and storage.
---

## How each class is supplied

| Options class | Supplied through | Mutability |
| --- | --- | --- |
| `CratisChoreographyOptions` | The argument of `AddCratisChoreography` | Init-only |
| `JobsOptions` | A registered `IOptions<JobsOptions>` instance | Init-only |
| `WorkflowsOptions` | `CratisChoreographyOptions.ConfigureWorkflows` or the `configure` callback of `AddCratisChoreographyWorkflows` | Settable |
| `EventSourcedWorkflowsOptions` | The `configure` callback of `AddEventSourcedWorkflows` | Settable |
| `EventSourcedStateMachinesOptions` | The `configure` callback of `AddEventSourcedStateMachines` | Settable |
| `PatchingOptions` | `CratisChoreographyOptions.ConfigurePatching` or the `configure` callback of `AddCratisChoreographyPatching` | Settable |
| `MongoDBJobsStorageOptions` | The `configure` callback of `AddCratisChoreographyMongoDBJobsStorage` | Settable |
| `MongoDBPatchStorageOptions` | The `configure` callback of `AddCratisChoreographyMongoDBPatchStorage` | Settable |
| `SqlJobsStorageOptions` | The required `configure` callback of `AddCratisChoreographySqlJobsStorage` | Settable |
| `SqlPatchStorageOptions` | The required `configure` callback of `AddCratisChoreographySqlPatchStorage` | Settable |

## CratisChoreographyOptions

Namespace `Cratis.Choreography.Hosting`. See [Host the Choreography silo](../hosting/index.md).

| Option | Default | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | When `false`, `AddCratisChoreography` configures nothing. |
| `Clustering` | `ClusteringMode.Localhost` | Localhost clustering, or durable MongoDB cluster membership. |
| `ClusterId` | `"default"` | The cluster id every instance joins. |
| `ServiceId` | `"default"` | The service id every instance joins. |
| `DatabaseName` | `"orleans"` | The MongoDB database for cluster membership and, with MongoDB storage, the grain storage providers. |
| `ConnectionString` | `"mongodb://localhost:27017"` | The MongoDB connection string used for clustering and grain storage. |
| `Storage` | `GrainStorageMode.MongoDB` | Where grain state lives. `InMemory` keeps jobs, job steps and workflow runs in the silo's memory. |
| `Workflows` | `false` | Hosts the workflow engine: its grain storage, journal, sweep and node execution. |
| `ConfigureWorkflows` | `null` | Configures `WorkflowsOptions` when `Workflows` is on. |
| `Patching` | `false` | Applies patches when the silo becomes Active. With in-memory storage, the applied-patch record is in memory too. |
| `ConfigurePatching` | `null` | Configures `PatchingOptions` when `Patching` is on. |

`CratisChoreographyOptions.Local()` returns `Clustering = Localhost` and `Storage = InMemory`.

## ClusteringMode and GrainStorageMode

| Enum | Value | Meaning |
| --- | --- | --- |
| `ClusteringMode` | `Localhost` (0) | Localhost clustering for a single instance. |
| `ClusteringMode` | `MongoDB` (1) | Durable MongoDB cluster membership for more than one instance. |
| `GrainStorageMode` | `MongoDB` (0) | Durable grain state in MongoDB. |
| `GrainStorageMode` | `InMemory` (1) | Grain state in memory for one silo; nothing survives the process. |

## JobsOptions

Namespace `Cratis.Choreography.Jobs`. Values below 1 are treated as 1 for `MaxConcurrentRehydration`, `MaxConcurrentCleanup`,
`MaxTerminalJobsPerCleanup`, `MaxConcurrentStepStarts`, `MaxActiveStepsPerJob`, `MaxStepAttempts` and `StepDeliveryMaxAttempts`.

| Option | Default | Meaning |
| --- | --- | --- |
| `MaxParallelSteps` | `null` | Steps one silo runs at once. `GetEffectiveMaxParallelSteps()` returns it, or the processor count minus 1 with a minimum of 1. |
| `DeadJobThreshold` | 1 hour | How old a job stuck in preparation with no steps must be before cleanup removes it. |
| `CleanupCadence` | 1 hour | How often the scavenger looks for dead jobs. |
| `MaxInlineStepPlanSize` | 1,048,576 bytes | Serialized size of a job's whole step plan above which the plan is kept outside the job document, when storage and every silo support it. Zero keeps every plan outside whenever possible. |
| `StepCheckpointBatchInterval` | 100 | Reported batches a step accumulates before its progress checkpoint is persisted. |
| `StepCheckpointFlushInterval` | 5 seconds | How long a step may hold an unpersisted checkpoint before it is flushed; zero or less disables the timed flush. |
| `MaxConcurrentRehydration` | 8 | Stored jobs resumed at once when the jobs manager rehydrates, oldest first. |
| `MaxConcurrentCleanup` | 4 | Dead jobs the cleanup deletes at once. |
| `TerminalJobRetention` | 7 days | How long completed and failed jobs stay before cleanup removes them with their steps; `Timeout.InfiniteTimeSpan` keeps them. |
| `MaxTerminalJobsPerCleanup` | 100 | Most terminal jobs selected, oldest first, in one cleanup pass. |
| `MaxConcurrentStepStarts` | 16 | Steps of one job started at once when it starts or resumes. |
| `MaxActiveStepsPerJob` | `null` | Steps of one job active at once; `null` starts every step up front. `Job.MaxActiveSteps` overrides it per job. |
| `MaxStepAttempts` | 1 | Attempts before a step's failure counts as the job's; 1 never retries. `Job.ShouldRetryStep` decides per failure. |
| `StepRetryBackoff` | 5 seconds | Delay before the second attempt of a failed step; each further attempt doubles it. |
| `MaxStepRetryBackoff` | 5 minutes | Longest delay between two attempts of a step. |
| `JobWatchdogInterval` | 1 minute | How often a running job counts its steps in storage, finalizes itself when all are done and retries a failed resume hook; zero, negative or infinite turns it off. |
| `StepLeaseTimeout` | zero | Heartbeat silence after which the watchdog may restart a step whose owner silo has left the cluster; zero, negative or infinite turns leases off. |
| `StepDeliveryMaxAttempts` | 10 | Times a step delivers its persisted outcome to its job before leaving it to the job's watchdog. |
| `StepDeliveryRetryDelay` | 1 second | Delay before the second delivery of a step outcome; each further delivery doubles it. |
| `MaxStepDeliveryRetryDelay` | 30 seconds | Longest delay between two deliveries of a step outcome. |
| `StepStopTimeout` | 10 seconds | How long stopping a step waits for its work to settle before it answers `JobStepError.StopTimedOut`. Keep it below the Orleans response timeout. |

## WorkflowsOptions

Namespace `Cratis.Choreography.Workflows`. See [Host and start workflows](../workflows/host-and-start.md).

| Option | Default | Meaning |
| --- | --- | --- |
| `JobsScope` | `"workflows"` | Scope of the jobs manager that holds a run's wave jobs, for the default `IWorkflowExecutionScopes`. |
| `Scopes` | empty set | Scopes the sweep and wave-job recovery act on, besides those an `IWorkflowScopeSource` names and those the silo has seen. |
| `WakeRunsItself` | `true` | While a retry or wait deadline is pending, the run's grain keeps a timer and a reminder to act on time. |
| `Sweep` | `true` | Whether the sweep runs on this silo. |
| `SweepInterval` | 30 seconds | How often the sweep asks every active run of every known scope whether it has work. |
| `SweepStartupDelay` | 20 seconds | Delay after the sweep starts before its first pass. |
| `IdleBeforeResume` | 15 minutes | How long a run may be idle before the sweep takes it forward from its recorded history. |

## EventSourcedWorkflowsOptions

Namespace `Cratis.Choreography.Chronicle.Workflows`. See
[Chronicle journal and triggers](../workflows/chronicle-journal-and-triggers.md).

| Option | Default | Meaning |
| --- | --- | --- |
| `EventStore` | `"Default"` | Event store that run facts are appended to. |
| `NamespaceFor` | the scope's value | Maps a `WorkflowScope` to an event store namespace. |
| `ScopeFor` | the namespace's value | Maps an event store namespace back to a `WorkflowScope`, for event triggers. |
| `ReadModels` | `true` | Maintains the run listing and open waits as read models. |
| `EventTriggers` | empty, read-only | The event triggers declared so far. |
| `Schedules` | empty, read-only | The schedules declared so far. |
| `OnEvent<TEvent>(workflow, trigger)` | none | Declares that an event admits a run of a workflow. |
| `Schedule(schedule)` | none | Declares that a workflow starts on a schedule. |

## EventSourcedStateMachinesOptions

Namespace `Cratis.Choreography.Chronicle.StateMachines`. See [Event-sourced state machines](../state-machines/event-sourced.md).

| Option | Default | Meaning |
| --- | --- | --- |
| `Discover` | `true` | Discovers every concrete `EventSourcedStateMachine<TModel>` by type discovery. |
| `StateMachines` | empty set | State machine types registered in addition to the discovered ones. |
| `ModelCatchUpTimeout` | 30 seconds | How long a delivered event waits for a materialized model to catch up before the delivery fails. A passive model never waits. |
| `ModelCatchUpPollInterval` | 50 milliseconds | How often a materialized model is checked while waiting. |

## PatchingOptions

Namespace `Cratis.Choreography.Patching`. See [Patching](../patching/index.md) and the
[patching reference](../patching/reference.md).

| Option | Default | Meaning |
| --- | --- | --- |
| `HostVersion` | `null` | Version of the running host. When `null`, the informational version of the entry assembly without build metadata, falling back to its assembly version. An unset or `1.0.0` version is unknown: patches apply, but the store is not marked. |
| `MaxAttempts` | 5 | Times the host asks the patch manager to apply patches while waiting for it to move to a host at least as new, before the start fails. |
| `RelocationDelay` | 1 second | Wait before asking again; it doubles after every attempt. |

## MongoDBJobsStorageOptions

Namespace `Cratis.Choreography.Storage.MongoDB`. See [MongoDB storage](../storage/mongodb.md).

| Option | Default | Meaning |
| --- | --- | --- |
| `DatabaseNameResolver` | `null` | Function of scope and namespace that returns the database name. When `null`, `DatabaseNames.ForJobs` applies: `{scope}+jobs`, or `{scope}+jobs+{namespace}`. |
| `TransientRetryCount` | 5 | Retries after a transient error (wait queue full, timeout, connection failure); zero disables retrying. |
| `TransientRetryBaseDelay` | 200 milliseconds | Delay before the first retry; it doubles each retry and is randomized. |

## MongoDBPatchStorageOptions

Namespace `Cratis.Choreography.Storage.MongoDB`.

| Option | Default | Meaning |
| --- | --- | --- |
| `DatabaseName` | `"choreography"` | The one database that holds the patch records for the whole store. |

## SqlJobsStorageOptions

Namespace `Cratis.Choreography.Storage.Sql`. See [SQL storage](../storage/sql.md).

| Option | Default | Meaning |
| --- | --- | --- |
| `OptionsResolver` | required | Function of scope and namespace that returns the `DbContextOptions<JobsDbContext>`, which also picks the EF Core provider. |

## SqlPatchStorageOptions

Namespace `Cratis.Choreography.Storage.Sql`.

| Option | Default | Meaning |
| --- | --- | --- |
| `OptionsResolver` | required | Function that returns the `DbContextOptions<PatchesDbContext>`, which picks the database and the EF Core provider. |
