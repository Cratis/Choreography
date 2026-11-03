---
title: Chronicle journal and workflow triggers
description: Keep workflow run facts as events in Chronicle, and start or resume runs from events, schedules, signals and state machine transitions.
---

Starting every run by hand is rarely what a business process wants. An order is placed, a payment arrives, the clock
reaches 06:00, or a state machine enters a state, and a workflow should start or resume. The
`Cratis.Choreography.Chronicle` package provides those triggers. It also changes where a run's facts live: from one grain-storage
document to events in the Chronicle event log, where they can be read, projected and audited like any other event.

## Keep the journal in Chronicle

`AddEventSourcedWorkflows()` does everything `AddCratisChoreographyWorkflows` does, and replaces the journal and the execution
scopes with Chronicle's. The run is the event source, and its facts are appended with a concurrency scope on the tail and replayed on
activation.

```csharp
using Cratis.Choreography.Chronicle.Hosting;

builder.Host.UseOrleans(silo => silo.AddEventSourcedWorkflows(options =>
{
    options.EventStore = "orders";
    options.NamespaceFor = scope => scope.Value;
}));
```

The facts are plain records in the core package, which does not know Chronicle. The adapter therefore registers each with the
Chronicle client as an event type whose identifier is its bare type name, which keeps facts already in a store readable.
It also registers the run listing (`WorkflowRun`, maintained by a reducer) and the open waits (`OpenWorkflowWait`, a projection)
as read models, and a reactor for each declared event trigger. The wave jobs of a scope live in the jobs manager of its event
store namespace.

No grain storage is needed for the runs themselves, but the sweep's small per-scope run index still is: register the
provider named `workflows` (`WellKnownGrainStorageProviders.Workflows`) as well. `AddEventSourcedWorkflows` takes no `WorkflowsOptions`;
set those through `CratisChoreographyOptions.ConfigureWorkflows` or by calling `AddCratisChoreographyWorkflows` with a callback.

| Option | Default | Meaning |
| --- | --- | --- |
| `EventStore` | `Default` | The event store the facts are appended to |
| `NamespaceFor` | the scope's value | Maps a `WorkflowScope` onto an event store namespace |
| `ScopeFor` | the namespace's value | Maps a namespace back onto a scope |
| `ReadModels` | `true` | Maintain the run listing and the open waits |
| `OnEvent<TEvent>(...)` | none | Declare an event that admits a run |
| `Schedule(...)` | none | Declare a schedule that starts a run |

## Choose a trigger

| Trigger | How |
| --- | --- |
| Manually | `IWorkflows.Start(...)`, see [host and start](host-and-start.md) |
| When an event is appended | `options.OnEvent<TEvent>(workflow, (@event, context) => trigger)`: a reactor admits a run. Returning `null` skips the event |
| When a state machine enters a state | `this.StartWorkflow(workflow)` from a state's `OnEnter`: the machine is the run's subject and its key the correlation |
| On a schedule | `options.Schedule(new WorkflowSchedule(name, scope, workflow, CronExpression.Parse("0 6 * * 1-5")))` |
| By a signal | `IWorkflowSignals.Signal(scope, correlation, contract, payload, actor)` resolves the signal waits that match |

The first four start runs; a signal resumes one that is waiting. For every starting trigger the deduplication key decides whether it
starts a run again, and the workflow definition's subject kind must equal the trigger's.

## Start on an event

The reactor receives the event and its `EventContext` and builds the trigger. Derive the deduplication key from the event, so a
replayed event finds its run. The event's namespace becomes the scope of the run, through `ScopeFor`.

```csharp
options.OnEvent<OrderPlaced>("orders.fulfil", (@event, context) => new WorkflowTrigger(
    WorkflowTriggerKind.Manual,
    "orders-events",
    $"order-placed:{context.SequenceNumber.Value}",
    new("order", @event.OrderId),
    WorkflowContractReference.Any,
    "{}",
    [],
    @event.OrderId,
    WorkflowCausationKey.None,
    WorkflowActor.System,
    context.Occurred));
```

`OrderPlaced` is your own event type. The engine records the trigger kind in the run's history and does not act on it, so
choose the kind that describes the trigger best.

## Start on a schedule

A `CronExpression` has five fields, evaluated in UTC; [the reference](reference.md#cron-expressions) gives the grammar.
Every silo runs the schedule service every 15 seconds. The deduplication key of a scheduled run is the schedule name and the
minute it fell due, so the silos starting the same minute admit one run between them. Minutes that fell due while no silo was running
are started when the first one is, up to ten minutes back. Anything older is skipped. The run's subject kind is `schedule`, so the
workflow's definition must use that subject kind.

```csharp
options.Schedule(new WorkflowSchedule(
    "nightly-reconcile", WorkflowScope.Default, "orders.reconcile", CronExpression.Parse("0 6 * * 1-5")));
```

## Start from a state machine

From a state's `OnEnter`, `StartWorkflow` starts a run for the machine the state belongs to. The machine's type name is the
subject kind and its key the subject id, and the key is also the run's correlation, so a signal raised for the machine reaches the run.

```csharp
using Cratis.Choreography.Chronicle.Workflows;
using Cratis.Choreography.StateMachines;

public class Approved : State<OrderModel>
{
    public override async Task<OrderModel> OnEnter(OrderModel state)
    {
        await this.StartWorkflow("orders.fulfil");
        return state;
    }
}
```

The default deduplication key is the machine type, the state and the machine's key, so a state entered again, through a
retried transition or an activation that finds the machine in it, finds the run the first entry started. Pass a
`deduplication` key that is distinct for every entry that should run again. An optional `payload` and `scope` complete the signature. For an
event-sourced machine the scope comes from the machine's namespace through `ScopeFor`; otherwise it is the default scope.
A state whose machine is not a grain throws `StateMachineIsNotAGrain`. See [event-sourced state machines](../state-machines/event-sourced.md).

## Resume with a signal

A `Wait` node with kind `signal` holds the run until a signal arrives with the matching correlation and contract:

```csharp
var resumed = await signals.Signal(
    WorkflowScope.Default, "42", new WorkflowContractReference("payment-received", 1), "{}", "payments");
```

`Signal` resolves every open signal wait whose correlation equals the one given and whose contract equals the one given or is
`WorkflowContractReference.Any`, through the `resumed` outlet, and returns how many it resolved. The waits are found in the
`OpenWorkflowWait` read model, which can lag the run. A signal raised before the wait it is meant for is visible there resolves
nothing, and it is not remembered. Signals depend on that read model, so keep `ReadModels` on when you use them.

## Look at runs

The `WorkflowRun` read model lists each run's identity, status, subject, correlation and activity, and can lag. The run grain
stays authoritative: `IWorkflows.GetRun(...).GetSnapshot()` answers from the journal. Together with the event log, that is your
diagnostic surface for a run that did not do what you expected.
