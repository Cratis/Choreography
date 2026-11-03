---
title: Host the engine and start a workflow
description: Host the workflow engine in a silo, supply the performers and definitions it needs, start a run from application code and follow it to completion.
---

You have a graph in mind. Three things turn it into a running process: code that performs each `Action`, a source that
returns the workflow's definition, and a silo that hosts the engine. This page wires them and starts a run. It assumes
[the model](index.md); for what the engine does with the pieces, read [the runtime](runtime.md).

## Perform a capability

Node work is done by an `IWorkflowNodePerformer`, which the engine finds by the capability a node references. Dependencies
come from the container.

```csharp
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;

public class ReserveStockPerformer(IStockService stock) : IWorkflowNodePerformer
{
    public CapabilityReference Capability => new("orders.reserve-stock", 1);

    public async Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken)
    {
        // The idempotency key is the same for every attempt of this node execution:
        // use it to deduplicate the external effect.
        var reserved = await stock.Reserve(invocation.Input.Value, invocation.IdempotencyKey.Value, cancellationToken);
        return reserved
            ? WorkflowNodeOutcome.Succeeded("next", """{"reserved":true}""")
            : WorkflowNodeOutcome.Failed("no stock");
    }
}
```

`IStockService` stands for your own service. An outcome is one of four things: `Succeeded` through an outlet the node
declares, `Failed`, `Skipped`, or `Ambiguous`, which stops the run for a person to reconcile instead of repeating an
effect that may have happened. A `Succeeded` outcome whose outlet the node does not declare, or whose output is not JSON of
at most 32 KiB, fails the attempt. A performer that has to do several things in order, each recorded before the next
starts, implements `IStagedWorkflowNodePerformer` and names its operations.

You do not register performers. The engine discovers every implementation, using the registered instance when there is one and otherwise creating it
with the container supplying its dependencies. Two performers that claim the same capability and version throw
`MoreThanOneWorkflowNodePerformer`.

## Supply definitions

A definition source implements `IWorkflowDefinitions`: it says which workflows it owns and returns the active snapshot.
Sources are discovered the same way and combined. A workflow that no source owns is rejected with
`NoActiveVersion`, and a workflow owned by two sources throws `MoreThanOneWorkflowDefinitionSource`.

```csharp
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Graphs;

public class OrderWorkflows : IWorkflowDefinitions
{
    public bool Owns(WorkflowKey workflow) => workflow.Workflow.Value.StartsWith("orders.", StringComparison.Ordinal);

    public Task<WorkflowDefinitionSnapshot?> GetActive(WorkflowKey workflow) =>
        Task.FromResult<WorkflowDefinitionSnapshot?>(new(
            workflow.Workflow,
            "orders",
            1,
            "order",
            new WorkflowGraph(
                [
                    new("start", WorkflowNodeKind.Trigger, new("manual", 1), WorkflowNodeConfiguration.Empty, ["next"], WorkflowNodePolicy.Default),
                    new("reserve", WorkflowNodeKind.Action, new("orders.reserve-stock", 1), WorkflowNodeConfiguration.Empty, ["next", "failed"],
                        new(3, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(10), 1)),
                    new("settle", WorkflowNodeKind.Wait, new("workflows.wait.timer", 1), """{"kind":"timer","after":"00:00:01"}""", ["elapsed"], WorkflowNodePolicy.Default)
                ],
                [
                    new("start", "next", "reserve"),
                    new("reserve", "next", "settle")
                ]),
            "catalog-1",
            [new(new CapabilityReference("orders.reserve-stock", 1), "1.0.0")]));
}
```

The positional arguments are the workflow id, the owning area, the version, the subject kind (`"order"`), the graph, the
catalog version and the provider version each capability is pinned to. A source can load these from a database, build
them from code like this one, or derive them from another feature's facts.

## Host the engine

Switch the engine on in `AddCratisChoreography`, or add it to a silo you configure yourself:

```csharp
using Cratis.Choreography;
using Cratis.Choreography.Hosting;

// With the co-hosted silo
builder.AddCratisChoreography(new CratisChoreographyOptions { Workflows = true });

// Or on a silo you configure yourself
builder.Host.UseOrleans(silo =>
{
    silo.AddMongoDBGrainStorage(WellKnownGrainStorageProviders.Workflows);
    silo.AddCratisChoreographyWorkflows(options => options.Scopes.Add("tenant-1"));
});
```

`Workflows = true` also registers the grain storage provider named `workflows`; the manual silo registers it itself and
must host the [job system](../jobs/index.md), because wave jobs use it
([configure Orleans yourself](../hosting/configure-orleans-yourself.md) lists the registrations). Every registration the
engine makes is a `TryAdd`, so a journal or scope mapping you registered first is kept.

To run without a database for development and tests, combine in-memory storage with localhost clustering. `CratisChoreographyOptions.Local()`
gives that but leaves `Workflows` off, so write the options out:

```csharp
builder.AddCratisChoreography(new CratisChoreographyOptions
{
    Clustering = ClusteringMode.Localhost,
    Storage = GrainStorageMode.InMemory,
    Workflows = true
});
```

Nothing survives the process. These are the `WorkflowsOptions`, set through `ConfigureWorkflows` or the callback above:

| Option | Default | Meaning |
| --- | --- | --- |
| `JobsScope` | `workflows` | The jobs scope the wave jobs of a run are started in, for the default `IWorkflowExecutionScopes` |
| `Scopes` | empty | Scopes the engine sweeps and recovers jobs for, besides those a source names and those the silo has seen |
| `WakeRunsItself` | `true` | Keep a grain timer and reminder while a retry or wait deadline is pending |
| `SweepInterval` | 30 seconds | How often the sweep asks every unfinished run whether it has anything to do |
| `SweepStartupDelay` | 20 seconds | How long after start the first sweep runs |
| `IdleBeforeResume` | 15 minutes | How long a run may be idle before the sweep takes it forward from its history |
| `Sweep` | `true` | Whether the sweep runs on this silo at all |

## Start a run and follow it

`IWorkflows` is a thin facade over the grains. Starting by hand admits a `Manual` trigger:

```csharp
var admission = await workflows.Start(WorkflowScope.Default, "orders.fulfil", new WorkflowTrigger(
    WorkflowTriggerKind.Manual,
    "orders-api",
    "order-42",
    new("order", "42"),
    WorkflowContractReference.Any,
    """{"orderId":"42"}""",
    [],
    "42",
    WorkflowCausationKey.None,
    "someone",
    DateTimeOffset.UtcNow));
```

The arguments after the source are the deduplication key, the subject (kind and id), the payload contract and payload, the
evidence, the correlation key that signals match on, the causation and the actor. The subject kind must equal the
definition's, or the admission is rejected. `admission.Admitted` says whether it started and `admission.Rejections` says why not.

`workflows.GetRun(scope, admission.Run)` returns the run grain. `GetSnapshot()` shows where it stands:

```csharp
var run = workflows.GetRun(WorkflowScope.Default, admission.Run);
var snapshot = await run.GetSnapshot();
Console.WriteLine($"{snapshot.Status} {string.Join(",", snapshot.Executions.Select(e => $"{e.Execution.Node.Value}:{e.Status}"))}");
```

```text
Running start:Succeeded,reserve:Scheduled
Waiting start:Succeeded,reserve:Succeeded,settle:Waiting
Succeeded start:Succeeded,reserve:Succeeded,settle:Succeeded
```

The run left `Running` while the performer ran, sat in `Waiting` on the one-second timer and ended `Succeeded`. The
same grain also takes `Cancel(reason)`. A person's decision on an `Approval` node arrives as
`ResolveWait(wait, "approved", payload, actor)`, where the wait id comes from `snapshot.OpenWaits`; an approval resolves
through `approved` or `rejected`. With Chronicle, [signals and schedules](chronicle-journal-and-triggers.md) start and resume runs without
you holding the grain.
