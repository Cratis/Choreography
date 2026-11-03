---
title: Troubleshooting Choreography
description: Match an error message or log line from the silo, jobs, patching, storage, workflows or state machines to its cause and the fix.
---

Each row starts from what you see: an exception type with its message, or a log line. Message text is quoted from the source;
`{...}` marks a value the message fills in. For where logs and queries come from, see
[Observability](../hosting/index.md#observability).

## Startup and hosting

| Symptom | Cause | Fix |
| --- | --- | --- |
| Release build error `Microsoft.Orleans.Reminders resolved to X but Microsoft.Orleans.Server resolved to Y`, or `MissingMethodException` when the silo builds its grain services | The two Orleans packages resolve to different versions. `Orleans.Providers.MongoDB` asks for a 9.x Reminders package. | Pin `Microsoft.Orleans.Reminders` to the same version as `Microsoft.Orleans.Server` in `Directory.Packages.props`, and keep its direct `PackageReference`. |
| The first `RegisterOrUpdateReminder` never returns, the calling grain times out, and no reminder ever ticks | The 10.x reminder service is pointed at the 9.x MongoDB reminder table. | Use `UseInMemoryReminderService()` and never register the MongoDB reminder table. See [Reminders and recovery](../hosting/reminders-and-recovery.md). |
| The silo starts, and the first job fails with `No service for type 'Cratis.Choreography.Storage.IJobsStorage' has been registered` | No jobs storage is registered. The providers read `IJobsStorage` when first used. | Register `AddCratisChoreographyMongoDBJobsStorage`, `AddCratisChoreographySqlJobsStorage` or an in-memory storage. |
| Start fails with `Unable to resolve service for type 'Cratis.Choreography.Storage.Patching.IPatchStorage' while attempting to activate 'Cratis.Choreography.Patching.PatchManager'` | `Patching` is on, but no patch storage is registered and `Storage` is not `InMemory`. | Register a patch storage. See [Choose patch storage](../patching/choose-patch-storage.md). |
| Every grain call produces two spans | `AddActivityPropagation()` ran after `AddCratisChoreography`, so the filters were registered twice. | Remove the call, or move it before `AddCratisChoreography`. |
| A tenant-scoped service inside a job resolves the wrong tenant | No `ITenancy` filters on the silo. | Add `AddTenancy<TTenancy>()`. See [Tenancy](../tenancy.md). |
| `TypeLoadException: Unable to resolve type alias ("inv", ..., "<hash>")` | Two releases share a cluster and one calls a grain method the other does not have. Orleans identifies a method by a hash of its interface, name and parameter types. | Finish the rolling upgrade, and start jobs through `IJobs`. See [Run jobs while two releases share a cluster](../upgrading/mixed-release-clusters.md). |

## Patching

| Symptom | Cause | Fix |
| --- | --- | --- |
| `PatchManagerCouldNotRelocate`: `The patch manager could not relocate to a host at least as new as {hostVersion}. Patches were not applied; an older host in the cluster is still holding the patch manager.` Log: `Patch manager could not relocate to a host at least as new as {HostVersion}` (Critical) | An older host kept the patch manager for longer than `MaxAttempts` waits starting at `RelocationDelay` and doubling. | Let the older silos drain, then start again. Raise `MaxAttempts` or `RelocationDelay` when a rolling upgrade is slow. |
| `Skipping patches on host {HostVersion}: host {RecordedVersion} has already patched this store` | A host older than the one that patched the store started. | None. This is the intended guard; the older host does not patch. |
| `Host version {HostVersion} is unknown; bypassing the patch version gate without recording a host version` | The entry assembly has no usable version, or it is `1.0.0`. | Set a real version on the host assembly, or set `PatchingOptions.HostVersion`. |
| `Failed to apply patch {PatchName}` and the silo does not start | The patch threw. Nothing is recorded for it until it succeeds. | Fix the patch and start again. It runs again from the beginning. |

## Jobs and placement

| Symptom | Cause | Fix |
| --- | --- | --- |
| `NoEligibleJobWorker`: `No silo is eligible to host job grain '{grainId}': {reason}` | Your placement director found no silo to host a step worker. A job being prepared ends failed with this message. | Start a silo that qualifies, or publish the silo metadata your director reads. See [Placement](../jobs/placement.md). |
| `JobStepPlacementCannotUseItselfAsDefault` | The cluster default placement strategy is itself a job placement strategy. | Configure another cluster default, or register your own director with `AddJobStepPlacement<TDirector>()`. |
| Warning `Silo metadata '{MetadataKey}' has the value '{Value}', which is not a positive whole number; using the configured job step parallelism instead` | The step limit published in silo metadata is not a positive integer. | Publish a positive integer, or accept `JobsOptions.MaxParallelSteps`. |
| `MissingStateCopier`: `No DeepCopier<{State}> is registered. A job step hands its work a copy of its state.` | The silo has no Orleans serializer registration for the step state, as in a test silo built without `AddSerializer`. | Register a `DeepCopier<T>` for the state type, or build the silo the normal way. |
| `JobStepRequestCannotRoundTrip`: `The request for step '{stepId}' cannot round-trip with the job's JSON serializer options.` | A step request cannot be written to JSON and read back, which recovery needs. | Change the request so System.Text.Json can write it and read it back with the job's options. |
| `JobTypeMustHaveARequestType` or `JobTypeCanOnlyHaveOneRequestType` | A job class has no request type, or more than one. | Give the job exactly one `IJobRequest` type. |
| `JobTypeAlreadyExists`: `JobType {jobType} is already associated with {existing} so it cannot be associated with {new}` | Two job classes share a job type name, for example a copied class with the same `[JobType]`. | Give each job class its own name. |
| `UnknownClrTypeForJobType`: `Unknown job type '{type}'` | Storage holds a job whose class is not loaded on this silo. | Deploy the assembly that contains the job class and keep its `[JobType]` name stable. |
| `JobGrainStorageProviderError`: `Error while performing {method} with job state type {type} : {error}` (steps raise `JobStepGrainStorageProviderError`) | The jobs storage returned an error for the operation. | Read the storage logs next to it. The job warnings `Job failed to persist new job status {Status}` and `An error occurred while writing state` have the same cause. |
| `Job watchdog failed`, `Job watchdog could not count the steps in storage` | The watchdog failed, usually while reading step state from storage. | Check that storage is reachable. The watchdog runs again after `JobWatchdogInterval`. |

## Recovery

| Symptom | Cause | Fix |
| --- | --- | --- |
| Warning `Jobs recovery is incomplete: only manager keys seen on this silo are known` | No `IJobsManagerKeys` provider of yours exists. | Supply a singleton provider. See [Recover jobs after a silo is lost](../jobs/recovery.md). |
| `Failed to resolve or enumerate jobs manager keys from {Provider}`, then `Retrying jobs recovery for {Providers} providers and {Keys} manager keys: attempt {Attempt} after {Delay}` | A provider threw or could not be constructed, for example because it needs a request-scoped service. Recovery retries after 1 second, doubling to 1 minute. | Make the provider a singleton with no request or tenant-scoped dependencies. |
| The same error for `WorkflowJobsManagerKeys`, with `Unable to resolve service for type 'Cratis.Choreography.Workflows.Sweeping.WorkflowScopes'` | The workflow engine's key provider is discovered on every silo, but the engine is not registered on this one. | Register the engine with `Workflows = true` or `AddCratisChoreographyWorkflows`. |
| `Failed to recover jobs for {Key}` | The jobs manager for that scope and namespace failed to rehydrate, usually on a storage error. | Fix the storage problem. Recovery retries by itself. |
| Jobs wait after a restart | The jobs manager rehydrates at most `MaxConcurrentRehydration` jobs at a time, oldest first. | Wait, or raise the limit if your storage can take it. |

## Storage

| Symptom | Cause | Fix |
| --- | --- | --- |
| `InvalidDatabaseName`: `The MongoDB database name '{databaseName}' was composed from scope '{scope}' and namespace '{namespace}', but {reason}.` | The composed `{scope}+jobs+{namespace}` name is empty, longer than 63 bytes in UTF-8, or contains one of `/ \ . space " $ * < > : \| ?` or a NUL character. | Use legal ids, or set `MongoDBJobsStorageOptions.DatabaseNameResolver`. |
| Warnings `Transient storage error during {Operation}; retry {Attempt} of {MaxRetries}` and `Giving up on {Operation} after {Attempts} attempts` | MongoDB timeouts, connection failures or a full driver wait queue (`MongoWaitQueueFullException`). | Share one `IMongoClient`. Raise `MaxConnecting` for a cold-start burst, or lower `MaxConcurrentRehydration` and `MaxConcurrentCleanup`. |
| Warning `Skipping stored job {JobId} because job type {JobType} is not registered` | Storage holds a job whose type this silo does not know. | Deploy the assembly with the job class, or remove the job. |

## Workflows and state machines

| Symptom | Cause | Fix |
| --- | --- | --- |
| `MoreThanOneWorkflowDefinitionSource` or `MoreThanOneWorkflowNodePerformer` | Two sources define one workflow, or two performers claim one capability. | Keep one of each per workflow and capability. |
| `MalformedWorkflowGrainKey`: `'{key}' is not a workflow grain key - expected '{scope}/{id}'` | A run key does not have a scope and an id. | Build the key from a scope and an id. |
| `WorkflowRunCouldNotBeRecorded`, or `The workflow run sweep could not complete` | What a run decided could not be appended to its journal, or the sweep failed. | Check the journal: grain storage, or Chronicle for the event-sourced journal. |
| `InvalidCronExpression`: `'{text}' is not a valid cron expression: {problem}` | A schedule's cron expression does not have five fields or cannot be read. | Correct the expression. The schedule service logs `Scheduled workflows could not be started` when starting scheduled runs fails. |
| Waiting runs do not wake after the whole cluster restarted | Their reminders were in memory. | The sweep resumes idle runs after `IdleBeforeResume`. Keep `Sweep` on. |
| `InvalidTypeForState`, `UnknownStateTypeInStateMachine`, `UnknownCurrentState` | A state does not implement `IState<>`, a type is not part of the machine, or the stored state name matches no state. | Fix the state types, or migrate the stored state. |
| `TransitioningDuringOnLeaveIsNotSupported` | A state transitions from `OnLeave`. | Move the transition out of `OnLeave`. |
| `TypeIsNotAnEventSourcedStateMachine`, `TransitionEventIsNotAnEventType` | A registered type is not a concrete state machine, or a transition names a type that is not an event. | Register a non-abstract class that derives from `EventSourcedStateMachine<TModel>`, and mark the event with `[EventType]`. |
| `MissingProjectionForStateMachineModel`, `StateMachineModelAlreadyHasProjection`, `StateMachineHasNoEventInterest` | No projection defines the model, the model already has one, or the machine consumes no events. | Override `DefineProjection` or add model-bound attributes, but not both, and declare at least one transition. |
| `ModelDidNotCatchUp`: `The model '{type}' did not catch up with the event at sequence number {n} within {timeout}` | A materialized model lagged behind. Chronicle retries the failed partition. | Prefer a passive model, or raise `ModelCatchUpTimeout`. |
