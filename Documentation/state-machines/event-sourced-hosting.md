---
title: Host event-sourced state machines
description: Register event-sourced state machines with Chronicle, choose which are discovered, tune model catch-up, and handle read model lag and startup failures.
---

An event-sourced state machine only works if Chronicle delivers the right events to the right grain, in the right
namespace, after the model has caught up. Wiring that by hand means a projection registration, a reactor per state
machine and a wait for the read model, and forgetting any of them leaves a grain that silently never refreshes.
`AddEventSourcedStateMachines()` does all three from the state machine types alone. This page follows
[defining event-sourced state machines](event-sourced.md).

## Add it to the application

Call `AddEventSourcedStateMachines()` on the silo builder or the service collection, next to `AddCratisChronicle()`:

```csharp
using Cratis.Choreography.Chronicle.Hosting;

builder.AddCratisChronicle();
builder.Host.UseOrleans(silo => silo.AddEventSourcedStateMachines());
```

The registration hooks into the Chronicle client options, so it must be in place before the client is first
resolved. Every concrete `EventSourcedStateMachine<TModel>` that discovery finds is inspected when the client is
created, and its artifacts are registered with Chronicle's explicit registration API, so they reach every event store
and namespace the client connects to:

| The model's projection | What is registered |
| --- | --- |
| The state machine overrides `DefineProjection` | That projection |
| The model carries model-bound projection attributes Chronicle has not discovered | The model-bound read model |
| Chronicle already discovered a projection or model-bound read model for the model | Nothing - it is not registered twice |

Then **one reactor per state machine** is registered, identified by the state machine's full type name. It subscribes
to exactly the event types the state machine is interested in: every event type its projection refers to, together with
the events its transitions are declared for, with interfaces and base types expanded to the event types implementing
them. It never subscribes to all events. The reactor addresses the
grain by its class, so a state machine needs no grain interface of its own for delivery; the interface exists for
callers.

## Choose which state machines to register

By default every concrete state machine is discovered. To register only some, turn discovery off and list them:

```csharp
silo.AddEventSourcedStateMachines(options =>
{
    options.Discover = false;
    options.StateMachines.Add(typeof(OrderProcess));
});
```

`EventSourcedStateMachinesOptions` is set only through that callback:

| Option | Default | Meaning |
| --- | --- | --- |
| `Discover` | `true` | Register every concrete `EventSourcedStateMachine<TModel>` that type discovery finds |
| `StateMachines` | empty | State machine types to register in addition to the discovered ones; a type listed twice is registered once |
| `ModelCatchUpTimeout` | 30 seconds | How long a delivered event waits for a materialized model to catch up before the delivery fails |
| `ModelCatchUpPollInterval` | 50 milliseconds | How often a materialized model is checked while waiting |

:::caution[Describe the state machine from its type alone]
`DefineProjection` and `DefineTransitions` are called at startup on an instance created without running any
constructor, so the integration can learn which events the state machine needs before any grain exists. They must not
use fields, constructor parameters or injected services - only the builder they are given.
:::

## Read model lag

A **passive** model is computed from the event log when it is read, so it always includes the event Chronicle most
recently delivered. That is why passive models are recommended for state machines.

A **materialized** model is written by its projection's own observer, which may not have handled the delivered event
yet. Before refreshing the grain for an event the projection consumes, the reactor polls the projection every
`ModelCatchUpPollInterval` until it has handled the event's sequence number. If it has not within
`ModelCatchUpTimeout`, the delivery fails with `ModelDidNotCatchUp`, which Chronicle treats like any other failing
[reactor delivery](/chronicle/reactors/event-processing/) for that event source. A stale model therefore never decides
a transition.

An event only the transitions use is not waited for: the projection never handles it, and events reach the reactor in
order per event source, so every projected event before it has already been waited for. See
[eventual consistency in projections](/chronicle/projections/eventual-consistency/) for why a materialized model
trails the log.

## Startup and runtime failures

Failing at startup is deliberate: a state machine that silently never refreshes is far harder to find. Each of these
surfaces when the Chronicle client is created:

| Failure | Cause | Fix |
| --- | --- | --- |
| `StateMachineModelAlreadyHasProjection` | The state machine overrides `DefineProjection` for a model Chronicle already has a projection for | Remove `DefineProjection` to use the existing one |
| `MissingProjectionForStateMachineModel` | Nothing defines a projection for the model | Override `DefineProjection`, or add model-bound attributes to the model |
| `StateMachineHasNoEventInterest` | Neither the projection nor the transitions refer to a known event type | Project or declare a transition for at least one event |
| `TransitionEventIsNotAnEventType` | `On<TEvent>` names a type that is neither an `[EventType]` nor an interface or abstract type | Mark it with `[EventType]` or name an interface that event types implement |
| `TypeIsNotAnEventSourcedStateMachine` | A type in `StateMachines` is abstract, generic or does not derive from `EventSourcedStateMachine<TModel>` | List only concrete state machines |
| `SchemasAreNotAvailableWhileInspectingProjections` | `DefineProjection` needed an event type's schema, which is not available during inspection | Describe the projection without it |

These appear while the system runs:

| Failure | Cause |
| --- | --- |
| `ModelDidNotCatchUp` | A materialized model stayed behind the delivered event for `ModelCatchUpTimeout`; prefer a passive model |
| `InvalidStateMachineKey` | A grain key is not `{eventStore}/{namespace}/{eventSourceId}`, or the event store or namespace contains `/` |
| `ReadModelStorageIsNotBound` | The model storage was read before the grain activated, which binds it; create grains through Orleans, not `new` |

## Test an event-sourced state machine

The seam is `IStateMachineEventStores`. Give an Orleans TestKit silo (the `OrleansTestKit10` package) a substitute that returns an event store whose read model
you control, activate the grain with its key, and deliver an event through `Refresh`:

```csharp
using Cratis.Choreography.Chronicle.StateMachines;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Execution;
using NSubstitute;
using Orleans.TestKit;
using Xunit;

public class OrderProcessSpec
{
    [Fact]
    public async Task ApprovalMovesTheOrderToApproved()
    {
        var eventStore = Substitute.For<IEventStore>();
        eventStore.ReadModels.GetInstanceById<Order>(Arg.Any<ReadModelKey>())
            .Returns(new Order("Jane", OrderStatus.Placed));
        var eventStores = Substitute.For<IStateMachineEventStores>();
        eventStores.Get(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>()).Returns(eventStore);

        var silo = new TestKitSilo();
        silo.AddService(eventStores);
        var process = await silo.CreateGrainAsync<OrderProcess>("orders/tenant-a/order-1");

        eventStore.ReadModels.GetInstanceById<Order>(Arg.Any<ReadModelKey>())
            .Returns(new Order("Jane", OrderStatus.Approved));
        var context = EventContext.From(
            "orders", "tenant-a", typeof(OrderApproved).GetEventType(), EventSourceType.Default,
            "order-1", EventStreamType.All, EventStreamId.Default, 7UL, CorrelationId.New());
        await process.Refresh(StateMachineRefresh.For(typeof(OrderApproved), context));

        Assert.IsType<Approved>(await process.GetCurrentState());
    }
}
```

The second `Returns` stands in for the projection having handled the event. [Testing](../testing/index.md) covers
cluster fixtures for the full path through the reactor.

## When it is the wrong fit

- **A process that decides, rather than follows.** Transitions here react to facts that already happened. When the
  grain must accept or reject a command, validate it where the event is appended - a command or a constraint - and
  let the state machine follow the outcome.
- **State that is not derived from events.** Use a [state machine](index.md) with its own storage.
- **Work that must happen exactly once per event.** `OnModelChanged` and `OnEnter` can run again when Chronicle
  redelivers an event after a failure or a replay, and `OnEnter` runs on every activation. Make side effects
  idempotent, or use a reactor built for them.
