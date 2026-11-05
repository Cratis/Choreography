---
title: Patching API and options reference
description: Look up Choreography patching types, options, defaults, outcomes, and registration methods, including the members required by a custom IPatchStorage.
---

Types live in the `Cratis.Choreography.Patching` namespace of the `Cratis.Choreography` package, except `IPatchStorage`
(`Cratis.Choreography.Storage.Patching`, in `Cratis.Choreography.Storage`). `SemanticVersion` and `AppliedPatch` also belong
to `Cratis.Choreography.Patching` and ship in `Cratis.Choreography.Storage`. For the reasoning, read the
[overview](index.md).

## PatchingOptions

| Option | Default | Meaning |
| --- | --- | --- |
| `HostVersion` | informational version of the entry assembly, without build metadata | The version of the running host. When not set, unparseable or `1.0.0`, it is unknown: patches run, the store is not marked and the older-host gate is bypassed |
| `MaxAttempts` | `5` | How many times the host asks the patch manager while waiting for it to reach a host at least as new as itself |
| `RelocationDelay` | 1 second | Wait before asking again, doubled after every attempt up to 30 seconds |
| `MaxWait` | 15 minutes | Longest the host keeps asking while calls fail transiently, before it throws `PatchingDidNotComplete` |

## ICanApplyPatch

| Member | Meaning |
| --- | --- |
| `SemanticVersion Version { get; }` | The release that introduced the change. Patches run in ascending order; one not newer than the patched version never runs |
| `string Name { get; }` | Identifies the patch in the record. Defaults to `GetType().Name`; renaming applies the patch again. The SQL provider allows up to 256 characters |
| `Task Up()` | Applies the patch. Recorded as applied only after it completes, so it must be safe to run more than once |

`Name` is a default interface member, so reading it through a class variable needs the interface type:
`((ICanApplyPatch)patch).Name`.

## IPatchRunner

| Member | Meaning |
| --- | --- |
| `Task ApplyPatches(CancellationToken cancellationToken = default)` | Applies every pending patch and returns when the patch manager has completed |

Throws `PatchManagerCouldNotRelocate` when no host at least as new as this one takes over within `MaxAttempts`, throws
`PatchingDidNotComplete` when transient call failures outlast `MaxWait`, throws what a failing patch throws, and honors the
cancellation token between attempts and during the wait.

## IPatchManager

The grain behind the runner, with grain type name `cratis-choreography-patch-manager`, an integer key of `0` and local
placement preference. Hosts do not call it directly.

| Member | Meaning |
| --- | --- |
| `Task<PatchOutcome> ApplyPatchesFor(SemanticVersion caller)` | Applies pending patches unless the activation runs on an older host than the caller |

## PatchOutcome

| Value | Meaning |
| --- | --- |
| `Completed` | Patching completed, or was skipped because a newer host has already patched the store |
| `Relocate` | The manager is on an older host than the caller and is moving to the caller's silo; ask again |

## PatchManagerCouldNotRelocate

An `Exception` raised by `IPatchRunner.ApplyPatches` after the last attempt. Its message names the host version and states that
an older host is still holding the patch manager. The runner also logs it at `Critical` before it throws.

## PatchingDidNotComplete

An `Exception` raised by `IPatchRunner.ApplyPatches` when response timeouts, an unavailable silo or rejected messages kept the
call from completing for `MaxWait`. Its message names the host version and the time waited, and its inner exception is the last
failure seen. The runner logs it at `Critical` before it throws.

## AppliedPatch

`record AppliedPatch(string Name, SemanticVersion Version, DateTimeOffset AppliedAt)`, the record of one patch that has run.

## SemanticVersion

`record SemanticVersion(int Major, int Minor, int Patch, string PreRelease = "", string BuildMetadata = "")`. It implements
`IComparable<SemanticVersion>`, ordering by major, minor and patch, then pre-release (a pre-release sorts before the release);
build metadata is ignored in comparisons.

| Member | Meaning |
| --- | --- |
| `NotSet` | `0.0.0` |
| `Parse(string)` | Parses `major.minor.patch[-prerelease][+build]`; throws `ArgumentException` on an invalid string |
| `TryParse(string, out SemanticVersion?)` | Returns `false` instead of throwing |
| implicit conversion from `string` | `SemanticVersion version = "1.5.0";` parses the string |
| `<`, `<=`, `>`, `>=` | Compare by precedence |

## IPatchStorage

Implement this to keep the record somewhere else. There is one set of these facts per store.

| Member | Meaning |
| --- | --- |
| `Task<IEnumerable<AppliedPatch>> GetAll()` | Every applied patch |
| `Task<bool> Has(string patchName)` | Whether a patch has been applied |
| `Task Save(AppliedPatch patch)` | Records a patch; recording one that exists replaces its record |
| `Task<SemanticVersion?> GetPatchedVersion()` | Version of the newest patch applied, or `null` |
| `Task SetPatchedVersion(SemanticVersion version)` | Sets it |
| `Task<SemanticVersion?> GetHostVersion()` | Version of the newest host that patched the store, or `null` |
| `Task SetHostVersion(SemanticVersion version)` | Sets it |

Provided implementations: `InMemoryPatchStorage`, `MongoDBPatchStorage` and `SqlPatchStorage`.

## Registration methods

| Method | Receiver | Does |
| --- | --- | --- |
| `AddCratisChoreographyPatching(Action<PatchingOptions>? configure = null)` | `ISiloBuilder` | Adds the patching services and applies patches when the silo reaches the `Active` lifecycle stage |
| `AddCratisChoreographyPatchingServices(Action<PatchingOptions>? configure = null)` | `ISiloBuilder` | Adds the services and `IPatchRunner` only; you call the runner |
| `CratisChoreographyOptions.Patching` / `ConfigurePatching` | co-hosted options | Calls `AddCratisChoreographyPatching`; with in-memory grain storage it also registers `InMemoryPatchStorage` |
| `AddCratisChoreographyInMemoryPatchStorage()` | `IServiceCollection` | Registers `InMemoryPatchStorage` |
| `AddCratisChoreographyMongoDBPatchStorage(IMongoClient client, Action<MongoDBPatchStorageOptions>? configure = null)` | `IServiceCollection` | Registers `MongoDBPatchStorage`; keeps a client registered earlier |
| `AddCratisChoreographySqlPatchStorage(Action<SqlPatchStorageOptions> configure)` | `IServiceCollection` | Registers `SqlPatchStorage`; the callback must set `OptionsResolver` |

Storage options:

| Option | Default | Meaning |
| --- | --- | --- |
| `MongoDBPatchStorageOptions.DatabaseName` | `choreography` | Database that holds the patch records |
| `SqlPatchStorageOptions.OptionsResolver` | required | `Func<DbContextOptions<PatchesDbContext>>`, with no arguments |

For names of collections and tables see [Storage names](../reference/storage-names.md).
