---
title: Reminders and job recovery on the silo
description: Why the Choreography silo keeps Orleans reminders in memory, which features depend on them, and how jobs recovery finishes interrupted work.
---

Two startup behaviors surprise people who read the Orleans documentation first: the co-hosted silo never uses a durable reminder
table, and it resumes interrupted jobs without any reminder at all. This page explains both, so you know what survives a
restart and what you must supply.

## Reminders stay in memory

`Orleans.Providers.MongoDB` has no Orleans 10 release. The 9.5.0 package is a 9.x binary, and only its cluster membership and
grain storage are safe on the 10.x runtime. Pointing the 10.x reminder service at its 9.x reminder table makes the reminder
service stop responding: the first `RegisterOrUpdateReminder` never completes, the calling grain's request times out, and no
reminder-backed grain ever ticks.

`AddCratisChoreography` therefore calls `UseInMemoryReminderService()` in both clustering modes and never registers the MongoDB
reminder table. In-memory reminders work in both modes, but they do not survive a restart of the whole cluster.

If you configure Orleans yourself, do the same: call `UseInMemoryReminderService()` and leave out the MongoDB reminder table.

## What depends on a reminder

Almost nothing. The job system uses grain timers and its own watchdog, never reminders. The one reminder in Choreography belongs
to a workflow run that is waiting for a retry or a deadline. While `WorkflowsOptions.WakeRunsItself` is on, the run keeps a timer
and a one-minute reminder so it acts when the time comes. After a full cluster restart those reminders are gone. The workflow
sweep, which `WorkflowsOptions.Sweep` leaves on by default, then takes idle runs forward again from their recorded history
after `IdleBeforeResume`. See [Options](../reference/options.md) for both settings.

## What recovery does on startup

`AddCratisChoreography` registers a recovery participant that runs one stage after the silo becomes Active, off the startup
path, so a backlog of jobs never blocks the silo from starting. It asks the jobs manager of every known scope and namespace to
rehydrate, and asks again when Orleans reports a cluster member Dead. A failed pass is retried with a growing delay, starting at
one second and capped at one minute.

The participant finds scopes and namespaces from `IJobsManagerKeys` providers. Its built-in provider only knows managers that
were activated on the same silo, and it logs `Jobs recovery is incomplete` when no provider of yours exists. Supply a singleton
provider that lists every scope and namespace your jobs live in. The provider contract, an example and the full recovery
behavior are in [Recover jobs after a silo is lost](../jobs/recovery.md).

Workflow job keys participate in recovery when workflow services are enabled. Jobs-only silos do not need workflow services.

:::caution[An interrupted step may run twice]
Recovery cannot know whether a step finished its external effect before the silo died, so make external effects idempotent. A
durable stop is the exception: recovery treats it as stopped work, not as interrupted work.
:::

## Recovery when you configure Orleans directly

A silo built without `AddCratisChoreography` has no recovery until you add it:

```csharp
using Cratis.Choreography.Hosting;

builder.Host.UseOrleans(silo =>
{
    // Register the job system and its jobs storage in this builder as well.
    silo.AddCratisChoreographyJobsRecovery();
});
```

`AddCratisChoreographyJobsRecovery` registers the type discovery, the built-in key provider, the discovery of your
`IJobsManagerKeys` implementations and the recovery lifecycle participant. Every registration uses `TryAdd`, so calling it twice
or after your own registrations is safe. [Configure Orleans yourself](configure-orleans-yourself.md) shows the full set.

## Next steps

- [Recover jobs after a silo is lost](../jobs/recovery.md) covers rehydration, stops and leases in depth.
- [Troubleshooting](../reference/troubleshooting.md) lists the recovery log messages and their causes.
