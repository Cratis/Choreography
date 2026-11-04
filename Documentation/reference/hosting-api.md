---
title: Hosting API reference
description: Every Add and Use extension method Choreography ships, with its namespace, receiver, what it registers and what must already be in place.
---

Methods that extend `ISiloBuilder` register on the silo's service collection, so they take effect for that silo only. Methods
that extend `IServiceCollection` register on the application's services. Generic type names are shown in backticks.

## Silo hosting

Namespace `Cratis.Choreography.Hosting`, package `Cratis.Choreography`. See [Host the Choreography silo](../hosting/index.md).

| Method | Receiver | Registers | Prerequisites |
| --- | --- | --- | --- |
| `AddCratisChoreography(options?)` | `WebApplicationBuilder` | Orleans via `UseOrleans`: activity propagation, cluster identity, the Cratis serializers, `ITypes`, `IJobTypes`, `IJobs`, `IJobStepThrottle`, default `IOptions<JobsOptions>`, jobs recovery, clustering, grain storage, the `jobs` and `job-steps` providers, in-memory reminders. With `Workflows` or `Patching` set, it also adds those. Returns without registering anything when `Enabled` is `false`. | A jobs storage registration unless `Storage` is `InMemory`. |
| `AddCratisChoreographyJobsRecovery()` | `ISiloBuilder` | `ITypes`, the built-in `JobsManagerKeys`, discovery of `IJobsManagerKeys` implementations and the recovery lifecycle participant. All with `TryAdd`. | The job system and its jobs storage in the same silo. See [Reminders and recovery](../hosting/reminders-and-recovery.md). |
| `AddJobAdmission<TAdmission>()` | `ISiloBuilder` | `TAdmission` as a singleton `IJobAdmission`, once per implementation. | Register on every silo that can host the job types it concerns; a silo without one admits every job. |
| `AddCratisChoreographyWorkflows(configure?)` | `ISiloBuilder` | Forwards to the `IServiceCollection` overload. | A grain storage provider named `workflows`, and the job system, because wave jobs use it. |
| `AddCratisChoreographyWorkflows(configure?)` | `IServiceCollection` | `WorkflowsOptions`, the grain-storage `IWorkflowRunJournal`, `IWorkflows`, `IWorkflowExecutionScopes`, `WorkflowScopes`, the sweeper and its hosted service, node performers, definition sources, admission policies and their discovery. All with `TryAdd`. | Register your own `IWorkflowRunJournal` or `IWorkflowExecutionScopes` first to replace the default. |
| `AddCratisChoreographyPatching(configure?)` | `ISiloBuilder` | Everything `AddCratisChoreographyPatchingServices` registers, plus a lifecycle participant that applies patches when the silo becomes Active. | An `IPatchStorage`. Without one the start fails when the patch manager grain activates. |
| `AddCratisChoreographyPatchingServices(configure?)` | `ISiloBuilder` | `PatchingOptions`, `TimeProvider`, `ITypes`, `IPatchRunner` and discovery of `ICanApplyPatch` implementations. | The Cratis serializers, for a host that calls `IPatchRunner` itself. |

## Job placement and parallelism

Namespace `Cratis.Choreography.Hosting`, package `Cratis.Choreography`, receiver `ISiloBuilder`. See
[Placement](../jobs/placement.md).

| Method | Registers | Prerequisites |
| --- | --- | --- |
| `AddJobPlacement()` | The `JobsPlacementStrategy` and its default director, which keeps resource-optimized placement for job controllers. | Every silo runs this release, and the registration is identical on every silo. |
| `AddJobPlacement<TDirector>()` | The same strategy with `TDirector` replacing its `IPlacementDirector`, whichever call comes first. | As above. The director must answer promptly and not block. |
| `AddJobStepPlacement()` | The `JobStepsPlacementStrategy` and its default director, which places workers as the cluster default does. | As above. The cluster default must not itself be a job placement strategy. |
| `AddJobStepPlacement<TDirector>()` | The same strategy with `TDirector` replacing its director. A director throws `NoEligibleJobWorker` when no silo qualifies. | As above. |
| `UseJobStepParallelismFromSiloMetadata(metadataKey?)` | Replaces `IJobStepThrottle` with one that reads each silo's step limit from the silo metadata key `cratis.jobs.max-parallel-steps`, or the key you pass. A missing or invalid value uses `JobsOptions.MaxParallelSteps`. | Publish the value with Orleans `UseSiloMetadata`. Works in either order with `AddCratisChoreography`. |

## Tenancy

Namespace `Cratis.Choreography.Tenancy`, package `Cratis.Choreography`. See [Tenancy](../tenancy.md).

| Method | Receiver | Registers | Prerequisites |
| --- | --- | --- | --- |
| `AddTenancy<TTenancy>()` | `ISiloBuilder` | `TTenancy` as the singleton `ITenancy`, and the incoming and outgoing tenant call filters. | An `ITenancy` implementation. |

## Serialization

Package `Cratis.Choreography`. See [Serializers and grain keys](../serializers-and-keys.md).

| Method | Namespace | Receiver | Registers |
| --- | --- | --- | --- |
| `AddCratisChoreographySerializers()` | `Cratis.Choreography` | `IServiceCollection` | The Cratis JSON options, the OneOf, expando and LINQ serializers, the concept serializer, the Cratis type filter and the JSON fallback. Included in `AddCratisChoreography`. |
| `AddLinqCollectionCopier()` | `Cratis.Choreography` | `IServiceCollection` | The copier for LINQ iterator and collection expression types. |
| `AddCompleteSerializer<TSerializer>()` | `Cratis.Choreography` | `IServiceCollection` | `TSerializer` as codec, copier and type filter, one singleton per interface. |
| `AddConceptSerializer()` | `Cratis.Choreography.Serialization` | `IServiceCollection` | The `ConceptSerializer` as codec, copier and type filter. |

## Jobs and patch storage

Receiver `IServiceCollection` for all methods. See [Storage](../storage/index.md).

| Method | Namespace and package | Registers | Prerequisites |
| --- | --- | --- | --- |
| `AddCratisChoreographyMongoDBJobsStorage(client, configure?)` | `Cratis.Choreography.Setup` in package `Cratis.Choreography.Storage.MongoDB` | The `IMongoClient` you pass, `MongoDBJobsStorageOptions`, the custom serializers, `JobStateSerializer`, `IJobsStorage`, and Arc's MongoDB change-stream watcher. | You own and share the `IMongoClient`. |
| `AddCratisChoreographyMongoDBPatchStorage(client, configure?)` | `Cratis.Choreography.Setup` in package `Cratis.Choreography.Storage.MongoDB` | The client (kept if one is already registered), `MongoDBPatchStorageOptions` and `IPatchStorage`. | As above. |
| `AddCratisChoreographySqlJobsStorage(configure)` | `Cratis.Choreography.Setup` in package `Cratis.Choreography.Storage.Sql` | `SqlJobsStorageOptions` and `IJobsStorage`. | `configure` must set `OptionsResolver`. |
| `AddCratisChoreographySqlPatchStorage(configure)` | `Cratis.Choreography.Setup` in package `Cratis.Choreography.Storage.Sql` | `SqlPatchStorageOptions` and `IPatchStorage`. | `configure` must set `OptionsResolver`. |
| `AddCratisChoreographyInMemoryJobsStorage()` | `Cratis.Choreography.Storage` in package `Cratis.Choreography.Storage` | The in-memory `IJobsStorage`. | Nothing survives the process. |
| `AddCratisChoreographyInMemoryPatchStorage()` | `Cratis.Choreography.Storage` in package `Cratis.Choreography.Storage` | The in-memory `IPatchStorage`. | Nothing survives the process. |

## Chronicle integration

Namespace `Cratis.Choreography.Chronicle.Hosting`, package `Cratis.Choreography.Chronicle`. Each method has an
`IServiceCollection` overload and an `ISiloBuilder` overload that forwards to it.

| Method | Registers | Prerequisites |
| --- | --- | --- |
| `AddEventSourcedStateMachines(configure?)` | `EventSourcedStateMachinesOptions`, type discovery, the state machine definitions, event store resolution, the grain resolver, and a hook that registers each state machine's projection and reactor with the Chronicle client when it is created. | In place before the Chronicle client is first resolved. A state machine that cannot be inspected fails client creation. See [Event-sourced state machines](../state-machines/event-sourced-hosting.md). |
| `AddEventSourcedWorkflows(configure?)` | Everything `AddCratisChoreographyWorkflows` registers, then replaces the journal and execution scopes with Chronicle's, and adds workflow signals, the schedule hosted service and the hook that registers run facts, read models and a reactor per event trigger. | The Chronicle client, and the `workflows` grain storage provider for the run index. It takes an `EventSourcedWorkflowsOptions` callback only; configure `WorkflowsOptions` with `services.Configure`. See [Chronicle journal and triggers](../workflows/chronicle-journal-and-triggers.md). |
