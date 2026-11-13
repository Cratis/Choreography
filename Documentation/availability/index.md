---
title: Availability engine
description: Decide per request whether a part of your application exists for a tenant, user or system caller, with audiences, gate expressions and lazily resolved context values.
---

Authorization says who may do something. Availability says whether a part of the application is there at all for a given
audience: a commercial package, a preview, a switched-off capability. The availability engine in
`Cratis.Choreography.Availability` evaluates that decision for one request. It is plain .NET with no dependency on Arc,
so any host can use it.

The engine has three pieces:

| Piece | What it does | Page |
| --- | --- | --- |
| Gate evaluation | Evaluates `for`, `when` and `with` parts combined with `and`, `or` and parentheses. | [Evaluating gates](evaluating-gates.md) |
| Audiences | Resolves which audiences apply to a request, from the tenant, the user and the system caller. | [Audiences](audiences.md) |
| Context values | Resolves the values a gate reads, lazily and once per request. | [Request-scoped context values](context-values.md) |
| Manifest | Describes what the model declares as data, and expands gates into alternatives. | [The availability manifest](manifest.md) |

Three rules hold everywhere:

- **Unknown denies.** A value that is absent, or audiences that were not resolved, make a gate unknown, and an unknown
  gate is not open.
- **Absence is not failure.** A missing key or an empty query result is unknown. A store that is down or code that
  throws is an exception, so an outage is never shown as "not available".
- **Availability has its own reason.** A closed gate reports `AvailabilityReasons.NotAvailable`, which clients can tell
  apart from an authorization denial.

## Set it up

```csharp
using Cratis.Choreography.Availability;
using Cratis.Choreography.Availability.Audiences;

services.AddSingleton<IAudienceSource>(new InMemoryAudienceSource());
services.AddSingleton<IDeclaredAudiences>(new DeclaredAudiences("Plus", "Preview"));
services.AddSingleton(new AvailabilityCatalog());
services.AddSingleton(new ContextValueCatalog());
services.AddAvailability(options => options.EnvironmentName = environment.EnvironmentName);
```

`AddAvailability` registers `IAudienceResolver` and `IAvailabilityEvaluator`. Replace `InMemoryAudienceSource` with an
implementation of `IAudienceSource` over your own storage for a real product.
