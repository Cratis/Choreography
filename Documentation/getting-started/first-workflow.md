---
title: Run your first workflow
description: Define a three-node order workflow as a graph, start it from an endpoint and read its run back, all on an in-memory silo.
---

When an order is placed you reserve stock and then send a confirmation. Written as two calls in a request handler, the
process is invisible: nobody can see which step an order is on, a crash between the calls leaves it half done, and a
retried request does both again. A Cratis Choreography workflow describes the same process as a graph of nodes. The
engine runs it on grains, records every step as a fact, and treats a repeated trigger as the same run.

This lesson builds the smallest useful graph: a trigger, a node that reserves stock and a node that sends the
confirmation. It runs on an in-memory silo, so you need no database and nothing survives the process. You need a .NET
SDK; the packages target `net10.0` and `net11.0`.

## Create the project

```bash
dotnet new web -n Fulfilment && cd Fulfilment && dotnet add package Cratis.Choreography
```

## Write the work the nodes do

A node does its work through a **performer**, a class that names the capability it provides. The graph refers to the
capability by name and version, so the work and the process definition can change separately.

```csharp title="Performers.cs"
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;

namespace Fulfilment;

public class ReserveStock(ILogger<ReserveStock> logger) : IWorkflowNodePerformer
{
    public static readonly CapabilityReference Reference = new("orders.reserve-stock", 1);

    public CapabilityReference Capability => Reference;

    public Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reserving stock, idempotency key {Key}", invocation.IdempotencyKey.Value);
        return Task.FromResult(WorkflowNodeOutcome.Succeeded("next"));
    }
}

public class SendConfirmation(ILogger<SendConfirmation> logger) : IWorkflowNodePerformer
{
    public static readonly CapabilityReference Reference = new("orders.send-confirmation", 1);

    public CapabilityReference Capability => Reference;

    public Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken)
    {
        logger.LogInformation("Confirmation sent");
        return Task.FromResult(WorkflowNodeOutcome.Succeeded("next"));
    }
}
```

A performer reports an outcome. `Succeeded("next")` leaves the node through its `next` outlet. `Failed` lets the node
retry according to its policy. `Ambiguous` means the effect may or may not have happened, which stops the run for
reconciliation instead of repeating it. `invocation.IdempotencyKey` is the same for every attempt of one node
execution, so use it to deduplicate a real side effect.

## Define the workflow

A definition is a versioned snapshot of the graph. Each node declares the outlets it can leave through, and each edge
connects one node's outlet to another node.

```csharp title="OrderWorkflows.cs"
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Graphs;

namespace Fulfilment;

public class OrderWorkflows : IWorkflowDefinitions
{
    public const string FulfilOrder = "orders.fulfil";

    static readonly WorkflowDefinitionSnapshot _fulfilOrder = new(
        Workflow: FulfilOrder,
        Area: "orders",
        Version: 1,
        Subject: "order",
        Graph: new WorkflowGraph(
            Nodes:
            [
                new("start", WorkflowNodeKind.Trigger, new("trigger", 1), WorkflowNodeConfiguration.Empty, ["next"], WorkflowNodePolicy.Default),
                new("reserve", WorkflowNodeKind.Action, ReserveStock.Reference, WorkflowNodeConfiguration.Empty, ["next", "failed"], WorkflowNodePolicy.Default),
                new("confirm", WorkflowNodeKind.Action, SendConfirmation.Reference, WorkflowNodeConfiguration.Empty, ["next"], WorkflowNodePolicy.Default)
            ],
            Edges:
            [
                new("start", "next", "reserve"),
                new("reserve", "next", "confirm")
            ]),
        CatalogVersion: "1",
        Capabilities: []);

    public bool Owns(WorkflowKey workflow) => workflow.Workflow.Value == FulfilOrder;

    public Task<WorkflowDefinitionSnapshot?> GetActive(WorkflowKey workflow) =>
        Task.FromResult<WorkflowDefinitionSnapshot?>(_fulfilOrder);
}
```

The source answers two questions: which workflows it owns and what the active definition is. Here it owns one and
returns a constant, but a real source can load definitions from a database. `WorkflowNodePolicy.Default` gives a node
one attempt of at most ten minutes. The `reserve` node declares a `failed` outlet but no edge leaves it, so a failure
there fails the run. A graph needs exactly one `Trigger` node, and admission rejects a graph that breaks that rule or
has an edge that leaves a node through an outlet it did not declare.

## Host the engine and start a run

Workflows are off by default. Switch them on and use in-memory grain storage:

```csharp title="Program.cs"
using Cratis.Choreography.Hosting;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Triggers;
using Fulfilment;

var builder = WebApplication.CreateBuilder(args);

builder.AddCratisChoreography(new CratisChoreographyOptions
{
    Storage = GrainStorageMode.InMemory,
    Workflows = true
});

var app = builder.Build();

app.MapPost("/orders/{orderId}/fulfil", async (string orderId, IWorkflows workflows) =>
{
    var admission = await workflows.Start(
        WorkflowScope.Default,
        OrderWorkflows.FulfilOrder,
        new WorkflowTrigger(
            Kind: WorkflowTriggerKind.Manual,
            Source: "orders-api",
            Deduplication: $"order-{orderId}",
            Subject: new("order", orderId),
            Contract: WorkflowContractReference.Any,
            Payload: $$"""{"orderId":"{{orderId}}"}""",
            Evidence: [],
            Correlation: orderId,
            Causation: WorkflowCausationKey.None,
            Actor: "orders-api",
            OccurredAt: DateTimeOffset.UtcNow));

    return Results.Ok(new { admission.Admitted, run = admission.Run.Value });
});

app.MapGet("/runs/{runId}", async (string runId, IWorkflows workflows) =>
{
    var snapshot = await workflows.GetRun(WorkflowScope.Default, runId).GetSnapshot();
    return new
    {
        status = snapshot.Status.ToString(),
        nodes = snapshot.Executions.Select(execution => new { node = execution.Execution.Node.Value, status = execution.Status.ToString() })
    };
});

app.Run();
```

`IWorkflows` is registered for you. The performers and the definition source are found by convention, so nothing else is
registered. The scope is an opaque value that separates one tenant's workflows from another's; `WorkflowScope.Default`
is the single scope used here.

## Run it

```bash
dotnet run --urls http://localhost:5000
curl -X POST http://localhost:5000/orders/1001/fulfil
curl http://localhost:5000/runs/c6da6c90-3727-546e-99b7-ae472371716e
```

```text
{"admitted":true,"run":"c6da6c90-3727-546e-99b7-ae472371716e"}
{"status":"Succeeded","nodes":[{"node":"start","status":"Succeeded"},{"node":"reserve","status":"Succeeded"},{"node":"confirm","status":"Succeeded"}]}
```

The console shows each performer once:

```text
info: Fulfilment.ReserveStock[0]
      Reserving stock, idempotency key ac317477-c436-552a-b377-2ef8428c6f80
info: Fulfilment.SendConfirmation[0]
      Confirmation sent
```

Post the same order again. You get the same run id back and `Reserving stock` is not logged a second time, because the
run id is derived from the workflow and the trigger's `Deduplication` key. A trigger delivered twice finds the run the
first delivery started.

## See a failure

Change `ReserveStock` to return `WorkflowNodeOutcome.Failed("out of stock")` and fulfil another order. The run reports
`"status":"Failed"`, `reserve` is `Failed` and `confirm` is `Unreachable`, because it never started. The `reserve` node
declares a `failed` outlet, but no edge leaves it, so the failure has nowhere to go. Add an edge from `reserve` through
`failed` to another node and the run follows it: that node runs and the run itself ends `Succeeded`.

## What the framework did for you

- It checked the graph's structure before admitting the run.
- It recorded every step of the run, from admission to each node's success, as a fact in a journal. Whenever the run's
  grain activates it rebuilds its state from those facts, which is why a run can survive a restart once the journal is
  in durable storage.
- It ran each node's work as a [job](../jobs/index.md) with the node's retry policy, so a node is retried, stoppable and
  recoverable like any other job.

This lesson used the in-memory journal. Set `Storage` to its default, MongoDB, for durable runs, and read
[Workflows](../workflows/index.md) for waits, approvals, fan-out and the Chronicle-backed journal.

## Where to go next

- [Workflows](../workflows/index.md) explains the node kinds, the run's journal and how time and scopes work.
- [Choosing a building block](../choosing-a-building-block.md) helps you decide whether a graph is more than you need.
- [Run your first durable job](index.md) is the lesson to take if a flat list of steps is enough.
