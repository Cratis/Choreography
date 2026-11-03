---
title: Upgrading with two releases in one cluster
description: How rolling upgrades work when silos of two Cratis Choreography releases share a cluster, and the contract rules that keep their grain calls compatible.
---

A rolling upgrade replaces silos one at a time. For the minutes or hours it takes, silos running the old release and silos running the new release are members of the same Orleans cluster, and they call each other's grains: a job on one silo starts a step grain that happens to be activated on another.

Cratis Choreography is a library inside every one of those silos, so both releases carry it, at two different versions. Without a rule, a change that looks harmless in source (an extra parameter, a widened type) makes the old silo unable to decode a call from the new one, and the rollout stalls half-finished.

This page explains how a call is identified on the wire, the rules Choreography follows within a major version so that a mixed cluster works, and what you do as a product built on it. Operating a mixed cluster, such as starting and deleting jobs while two releases run, is covered in [Mixed-release clusters](mixed-release-clusters.md).

## How Orleans identifies a grain call

Orleans does not send a method name. It sends a hash computed from the declaring interface, the method name and the parameter types. The receiving silo looks that hash up in its own copy of the interface.

A change that every caller still compiles against can therefore be a different method on the wire. Adding an optional parameter to `Stop(JobId)` produces a new hash, and a silo of the earlier release has no method under it. The failure appears at the receiving silo as a `TypeLoadException` whose message starts with `Unable to resolve type alias`, not as a compile error anywhere.

## The rule: grain interfaces only grow within a major

Within one major version, a grain interface only gains members. Every grain interface in Choreography (anything deriving from `IGrainWith...Key` or `IGrainObserver`, including generic ones such as `IJob<TRequest>` that your own interfaces derive from) follows it:

| Change | What to do within a major |
| --- | --- |
| Add a method | Add it and put `[Version(n)]` on that interface, one higher than its current version. No attribute means version 0, so the first is `[Version(1)]`. |
| Add a parameter, or change a parameter or return type | Do not edit the method. Add an overload beside it, raise `[Version]`, and make the old method forward to the new one. |
| Remove or rename a method | Not allowed. Keep it serving and remove it in the next major. |
| Rename or move an interface or grain class | Not allowed. The full name is part of the wire identity. |
| Change a method body or its documentation | Nothing to do. The wire identity is unchanged. |

`IJob` and `IJobsManager` are at `[Version(1)]` in the manifest; every other grain interface is at version 0. `IJobStep` shows the overload pattern: it keeps `Prepare(object)` and offers `Prepare(object, JobStepStage)` beside it, each with its own wire identity in the manifest.

Raise `[Version]` only on the interface that gained a method. `[Version]` makes Orleans route a call that needs the new method to a silo that has it, instead of to an activation on an older silo. Raised on every interface at once, it would send every call from a new silo looking for a new silo and move every grain during a rollout.

```csharp
using Orleans.CodeGeneration;

[Version(1)]
public interface IJob : IGrainWithGuidCompoundKey
{
    // existing methods unchanged

    Task<Result<JobError>> Pause();
}
```

This excerpt shows the shape of the change, not the full `IJob`. Write the `using` rather than qualifying `Version` in place: inside a `Cratis.*` namespace the name `Orleans` resolves to `Cratis.Choreography`, not to Orleans.

## Types that travel between silos

Grain calls carry request, result, error and state types, and serializers write type names into every value.

- Never move or rename a serializer, or a type a serializer writes the name of. The names are on the wire.
- Request, result, error and state types only gain members. Renaming, retyping or removing a property, or changing the `[Id]` numbers of a `[GenerateSerializer]` type, breaks a silo of the other release.

Nothing checks these two automatically. Review them by hand when you change such a type.

## How the contracts are checked

Each project that declares grain interfaces keeps an `OrleansContracts.txt` next to its project file: `Source/Choreography` and `Source/Choreography.Chronicle` both do. The file is generated, never hand-edited, and records every grain interface with its `[Version]`, each method's wire identity next to its CLR signature, and every grain class. A line looks like this:

```text
interface [GrainInterfaceType("Cratis.Choreography.Patching.IPatchManager")] Cratis.Choreography.Patching.IPatchManager [Version(0)]
  0AB5CDA8: ApplyPatchesFor(Cratis.Choreography.Patching.SemanticVersion) -> Task<Cratis.Choreography.Patching.PatchOutcome>
```

`IPatchManager`, the grain that coordinates [patches](../patching/index.md), is in the manifest like every other grain, and is bound by the same rule. Two checks use the files:

- The Orleans contract analyzer, shipped with `Microsoft.Orleans.Sdk` and switched on with the `EnableOrleansContractsAnalyzer` MSBuild property, fails the build when the source and the manifest disagree. Choreography's projects also list the analyzer's diagnostics `ORLEANS0016` to `ORLEANS0025` and `ORLEANS0027` in `WarningsAsErrors`.
- The Grain Compatibility workflow compares the manifests with those of the latest release and fails when the change is not purely additive. A pull request labeled `major` is not checked, because a new major is upgraded with a full stop. The comparison is done by the [grain compatibility tool](grain-compatibility-tool.md).

Neither check looks inside request, result, error or state types.

## What a product built on Choreography does

- Within a major, upgrade Choreography silo by silo. Both releases satisfy the rule, so a mixed cluster works. Read [Mixed-release clusters](mixed-release-clusters.md) before you do it with jobs running.
- Treat a new major as a full stop: stop the cluster, then start it on the new major. Releases across a major boundary are not expected to share a cluster.
- Apply the same rule to your own grains. A job interface such as one deriving from `IJob<TRequest>` is a grain interface of yours, and a rolling upgrade of your product has the same wire constraints. Enable the analyzer in your project (`EnableOrleansContractsAnalyzer`), keep an `OrleansContracts.txt` beside it and run the [grain compatibility tool](grain-compatibility-tool.md) against your last release in CI.
- Treat your own request, result and state types as wire contracts: add members, never rename or retype.

:::note[Not every change is a rolling upgrade]
If you can stop the whole cluster before starting the new release, none of this applies to you. The rules exist for clusters that must keep serving while they upgrade.
:::
