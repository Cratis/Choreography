---
title: Availability engine (deprecated)
description: The availability engine moved to Cratis Arc. Cratis.Choreography.Availability is an obsolete forwarder that is removed in the next major release.
---

> **Deprecated.** The availability engine now lives in Cratis Arc, in the `Cratis.Arc.Availability` package. Everything
> this section used to describe - audiences, gate evaluation, request-scoped context values and the manifest - is
> documented there. `Cratis.Choreography.Availability` stays for one release as an obsolete forwarder and is then
> removed together with these pages.

Choreography keeps state machines, jobs, workflows and workers. It no longer owns availability.

## Move to Arc

1. Reference `Cratis.Arc.Availability`. `Cratis.Choreography` already depends on it, so a host that upgrades both
   packages together keeps working.
2. Change `using Cratis.Choreography.Availability;` to `using Cratis.Arc.Availability;`. The audience and context
   namespaces move the same way: `Cratis.Choreography.Availability.Audiences` becomes
   `Cratis.Arc.Availability.Audiences` and `Cratis.Choreography.Availability.Context` becomes
   `Cratis.Arc.Availability.Context`. Type names are unchanged.
3. Call `AddAvailability` (or `AddArcAvailability` for the Arc request pipeline) from the Arc package.

## What the forwarder does

The obsolete `AddAvailability` extension in `Cratis.Choreography` calls the Arc `AddAvailability`, so the engine that runs
is Arc's. These types derive from the Arc type of the same name and carry an `[Obsolete]` message that points to Arc:
`AvailabilityOptions`, `AvailabilityCatalog`, `AvailabilityEvaluator`, `InMemoryAvailabilityManifest`,
`CircularAvailabilityException`, `UndefinedAvailabilityException`, `EnvironmentSwitchRefusedException`,
`AudienceResolver`, `DeclaredAudiences` and `InMemoryAudienceSource`.

Types that cannot be forwarded - enums, static classes, interfaces, records and the expression types - are not
duplicated. Use the Arc ones directly. Options passed to the obsolete `AddAvailability` are copied to the Arc options
instance, so resolve `Cratis.Arc.Availability.AvailabilityOptions`, not the forwarder type.
