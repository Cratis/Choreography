---
title: Tenancy across grain calls
description: Carry your application's tenant into every job, step and grain call with ITenancy and AddTenancy, and avoid a silent split between tenants.
---

A multi-tenant application resolves its tenant from the web request: a header, a claim, a host name. A grain call has no request.
When a job step runs, anything it resolves that depends on "the current tenant" falls back to whatever your tenant resolution
does without a request, while the job's explicit writes go to the namespace its key names. Nothing fails. The two halves of the
same job quietly use different tenants, and the problem only shows once a second tenant exists.

Choreography closes that gap with two Orleans call filters. You tell them how your application represents a tenant, and they
re-establish it around every grain call that can name one.

## Implement ITenancy

`ITenancy` is the one contract you own. It lives in `Cratis.Choreography.Tenancy`:

| Member | What you answer |
| --- | --- |
| `Current` | The tenant the current logical flow definitely acts for, or `null` when none is established. The outgoing filter records it onto grain calls. |
| `TenantFor(scope, @namespace)` | The tenant a job's coordinates belong to, or `null` when they name no tenant. |
| `Establish(tenant)` | Makes `tenant` current for the logical flow until the returned scope is disposed, then restores the previous tenant. |

Choreography treats scope and namespace as opaque. Chronicle uses the event store name as the scope and the tenant's namespace
as the namespace; another host might use the tenant as the scope. Only you know the mapping, which is why `TenantFor` is yours.

```csharp
using Cratis.Choreography.Tenancy;

public sealed class AmbientTenancy : ITenancy
{
    static readonly AsyncLocal<string?> _tenant = new();

    public string? Current => _tenant.Value;

    public string? TenantFor(string scope, string @namespace) =>
        string.IsNullOrEmpty(@namespace) ? null : @namespace;

    public IDisposable Establish(string tenant)
    {
        var previous = _tenant.Value;
        _tenant.Value = tenant;
        return new Restore(previous);
    }

    sealed class Restore(string? previous) : IDisposable
    {
        public void Dispose() => _tenant.Value = previous;
    }
}
```

Hold the tenant in something that flows with the logical call, such as `AsyncLocal<T>`, and connect it to the same source your
tenant-scoped services read.

:::caution[Never answer a guess from Current]
Answer `Current` only for a tenant the flow actually knows: an ambient tenant a background flow established, or the tenant of
the request in flight. Do not return a deployment-wide fallback. The outgoing filter writes whatever you return onto the call,
and the receiving side treats it as knowledge.
:::

## Register the filters

`AddTenancy<TTenancy>()` extends `ISiloBuilder`. It registers your type as the singleton `ITenancy` and adds both call filters:

```csharp
using Cratis.Choreography.Tenancy;

builder.Host.UseOrleans(silo => silo.AddTenancy<AmbientTenancy>());
```

Register it on every silo. It is independent of `AddCratisChoreography`, so the order of the two calls does not matter.

## How a call gets its tenant

The incoming filter, `TenantIncomingCallFilter`, picks the tenant in this order:

1. **From the grain key.** For the jobs manager, job and job step grains, the key names a scope and namespace, and the filter
   asks `TenantFor` for the tenant. The key survives restarts, so a job resumed after a crash still runs for the tenant it was
   started for, and the key wins over anything the caller sent.
2. **From the caller.** For every other grain, the tenant rides the Orleans `RequestContext` entry `Cratis-Tenant-ID`, which the
   outgoing filter, `TenantOutgoingCallFilter`, wrote from `Current` on the way out. `TenantRequestContext.Key` holds the key name.
3. **Nothing.** A call that names no tenant either way runs exactly as before.

When the filter finds a tenant it calls `Establish(tenant)` and disposes the returned scope when the call ends.

## Tenancy and workflows

The workflow engine uses the same contract. With an `ITenancy` on the silo, the default `IWorkflowExecutionScopes` treats a
workflow scope as the tenant: entering a scope calls `Establish` with the scope's value, and a jobs manager key maps back to a
scope through `TenantFor`, falling back to the key's namespace. Without `ITenancy`, nothing is established. The
Chronicle variant maps through the event store and namespace the same way. See
[Host and start workflows](workflows/host-and-start.md).

## Next steps

- [Host the Choreography silo](hosting/index.md) shows where `AddTenancy` sits next to the rest of the silo setup.
- [Hosting API](reference/hosting-api.md) lists every registration method.
