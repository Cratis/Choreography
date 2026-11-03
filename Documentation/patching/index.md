---
title: Patching a store when a new release starts
description: How Choreography applies one-time data changes at startup, orders them by version and keeps old hosts from patching a newer store.
---

Releases change data as well as code. A new version renames a field in stored job state, backfills a column, or moves
records to a new shape. Without help, you write that change as a migration script, run it by hand, and hope every silo that
starts afterward finds the data in the shape it expects. During a rolling upgrade that hope fails: an old silo and a new silo
share one store for a while, and either can start work against it.

Patching moves that change into the release. You write a class that implements `ICanApplyPatch`, and when a host starts on a
version that carries it, Choreography applies it once per store, in order, before the silo takes work. Nothing is registered:
the patch is discovered, and the container supplies its dependencies.

## What a patch is

A patch is a one-time change to the data a host owns. It has a `Version` (the release that introduced the change), a `Name`
that identifies it in the record of applied patches, and an `Up()` method that makes the change. Use one for a data change that
must happen exactly when a release arrives and must not be repeated: backfilling, reshaping stored state, retiring a field.

It is the wrong fit for:

- **A schema change your database tooling already owns.** SQL migrations for Choreography's own tables run automatically; use
  EF Core migrations for your own tables.
- **Work that is allowed to take hours.** A failing patch stops the host from starting, and a long one delays every start.
  Start a [job](../jobs/index.md) from the patch or run the backfill separately.
- **A change you want to run on every start.** A patch is recorded and never runs again; use a startup task.

## What the framework does that you cannot see

Two versions are kept in the store, and they do different jobs:

- **Patched version** is the version of the newest patch applied. A patch whose `Version` is not newer than this is never
  applied, even if it has no record of its own.
- **Host version** is the version of the newest host that has patched the store. A host older than this applies nothing,
  because only the newest host knows the newest patches.

When the silo becomes active, the host asks one cluster-wide grain, the patch manager, to apply patches. The manager:

1. Compares the caller with itself and relocates when it is older (next section).
2. Skips everything when the store's host version is newer than its own, logging a warning.
3. Otherwise selects the patches newer than the patched version, drops those whose name is already recorded, and runs them in
   ascending `Version` order.
4. Records each patch after its `Up()` completes, then sets the patched version to the newest it applied.
5. Marks the store with its host version.

## Relocation during rolling upgrades

The patch manager activates on the silo that first asks, and Orleans keeps the activation. In a mixed cluster, that can be a
silo running the old release, whose patch set lacks the newest patches. If it patched, the new release would start against
data nobody had patched.

So when a newer host calls an older manager, the manager answers `PatchOutcome.Relocate`. It migrates itself to the caller's
silo, using a hint the caller sent in the request context, and deactivates when idle if there is no hint. `IPatchRunner` on the
new host keeps asking. It waits `RelocationDelay` (one second by default), doubles the wait after every attempt up to 30
seconds, and gives up after `MaxAttempts` (five by default) answers of "relocate". Giving up throws
`PatchManagerCouldNotRelocate` and the host does not start. With the defaults the runner waits 1, 2, 4 and 8 seconds between
its five attempts, 15 seconds in all. If an older silo keeps the manager busy for longer than that, raise `RelocationDelay` or
`MaxAttempts`.

You never scale the old release down to make room. The new pods start while the old ones keep serving, the manager moves to the
first new pod that asks, and the old pods that restart later find the store marked with the newer host and apply nothing.

The manager answers for the host it runs on. When an older host asks while the manager sits on a newer host, the manager
applies the pending patches of that newer host, or finds them already applied, and answers `Completed`. The older host starts
against a store the newer release has patched. A manager on an older host never patches a store that a newer host has already
marked: it logs a warning and answers `Completed` without applying anything.

## Calls that fail while nothing is wrong

A rolling deployment also loses pods, and a patch can take longer than Orleans lets one grain call wait. Neither is a failed
patch. The runner treats Orleans' own response timeout, an unavailable silo and a rejected message as transient: it asks again
with the same back-off, which either waits for the patch that is still running in the manager or reaches a manager that has
been re-created on a silo that is alive. A patch records only after it ran, so a patch that was interrupted is attempted again,
which is why `Up()` must be safe to repeat.

These retries are bounded by `MaxWait` (15 minutes by default), the longest a host waits in total. When it runs out,
`IPatchRunner.ApplyPatches` throws `PatchingDidNotComplete`, keeping the last failure as its inner exception. A timeout raised
by a patch itself is a failing patch, not a transient one, and fails the start at once. Set the Orleans
`SiloMessagingOptions.ResponseTimeout` and `MaxWait` above the longest patch you ship.

## Unknown host versions

The host version defaults to the informational version of the entry assembly, without build metadata. When it is not set, or
is `1.0.0` (the version an unversioned build carries), the version is unknown. Patches still run, but the store is not marked
with a host version and the older-host gate is bypassed, so a development build never blocks a real release. Relocation only
happens when both the manager and the caller have known versions. A warning is logged in the unknown case.

Set `PatchingOptions.HostVersion` explicitly if your entry assembly does not carry the version you ship.

## Failure semantics

A patch that throws stops the start. The failure is logged as an error naming the patch, `IPatchRunner.ApplyPatches` throws,
and the silo does not finish starting. This is deliberate: a host that carries on with a half-applied change would run on data
it was not written for.

Patches before the failing one stay recorded and are not repeated on the next start. The failing one is attempted again, so
`Up()` must be safe to run more than once. See [Apply a patch](apply-a-patch.md) for the contract.

## Where to go next

- [Apply a patch](apply-a-patch.md) writes, turns on and runs a patch end to end.
- [Choose patch storage](choose-patch-storage.md) picks where the record of applied patches lives.
- [Patching reference](reference.md) lists every type, option and default.
- [Mixed-release clusters](../upgrading/mixed-release-clusters.md) covers the rest of a rolling upgrade.
