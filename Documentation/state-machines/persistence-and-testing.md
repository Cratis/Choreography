---
title: Persist, restore and test a state machine
description: Control which state a grain activates into, supply its own storage, transition by type, and test states and the grain without a cluster.
---

A state machine that forgets where it was after a restart is worse than a status field, and one that restores the
wrong state does work in a state it should have left. This page covers what Cratis Choreography records, how the
grain finds its state again, the extension points for storage that is not a configured provider, and how to test it
all. It builds on the [states and transitions](index.md) of a plain state machine.

## Persist and restore the current state

When the stored state derives from `StateMachineState`, the state machine records the full name of the current
state's type in `CurrentState` after every transition. On activation it enters that state again; when nothing is
recorded it enters `InitialState`. A grain whose stored state does not derive from `StateMachineState` always
activates into `InitialState`, unless you override `ResolveActivationState()` as described below.

:::caution[A recorded state name is a type name]
`CurrentState` holds a type name, so renaming or moving a state type changes what is recorded. A grain that
activates with a recorded name no state matches fails with `UnknownCurrentState`. Migrate the stored value when you
rename a state.
:::

On deactivation, the state machine leaves the current state by calling its `OnLeave`, and writes the stored state.
It does not transition anywhere and runs none of the `On…State` hooks; the recorded current state is what the grain
activates into next time.

Activation runs in this order:

1. With [supplied storage](#supply-the-storage), the machine reads the stored state first.
2. `OnActivation` - override it to prepare anything the states need.
3. `CreateStates()` - every state deriving `State<TStoredState>` gets its `StateMachine` set, whichever assembly it is defined in.
4. `ResolveActivationState()` picks the state to enter.
5. The machine transitions into that state, running its `OnEnter` as for any other transition.

Because step 5 is an ordinary transition, `OnEnter` runs again after every restart, deactivation and move to another
silo. Make enter behavior safe to repeat.

## Supply the storage

The protected `StateMachine(IStorage<TStoredState> storage)` constructor gives the grain its storage directly, with
no `[StorageProvider]` attribute. Orleans does not read supplied storage as part of the grain lifecycle, so the state
machine calls `ReadStateAsync()` at the start of activation, before `OnActivation`. Every write goes to the supplied
storage.

```csharp
using System.Collections.Immutable;
using Cratis.Choreography.StateMachines;
using Orleans.Core;

public interface IExternalDocument : IGrainWithGuidKey
{
    Task Publish();
}

public class ExternalDocument(IStorage<DocumentState> storage) : StateMachine<DocumentState>(storage), IExternalDocument
{
    protected override Type InitialState => typeof(Draft);

    public override IImmutableList<IState<DocumentState>> CreateStates() =>
        ImmutableList.Create<IState<DocumentState>>(new Draft(), new Published(), new Archived());

    public Task Publish() => TransitionTo<Published>();
}
```

The storage is resolved like any other grain constructor dependency, so register an `IStorage<DocumentState>`
implementation, or have the derived constructor build one and pass it to the base constructor.

## Decide the state to activate into

Override `ResolveActivationState()` when the current state follows from the stored state itself, such as an
authoritative model, rather than from the recorded `CurrentState`:

```csharp
protected override Type ResolveActivationState() =>
    State.PublishedAt == default ? typeof(Draft) : typeof(Published);
```

The returned type must be one of the states the machine created, and it is entered through `OnEnter` like any other
transition. A type that is not an `IState<TStoredState>` fails activation with `InvalidTypeForState`, and one the machine did not
create fails it with `UnknownStateTypeInStateMachine`.

## Transition to a state known only by type

When the target comes from data - a table of transitions, a configuration - use the protected
`TransitionTo(Type stateType)` and `CanTransitionTo(Type stateType)`. They ask the current state through the same
`CanTransitionTo<TTargetState>`, and behave exactly as their generic counterparts. A type that is not a state of the
machine's stored state throws `InvalidTypeForState`, and a state type the machine did not create throws
`UnknownStateTypeInStateMachine`.

## Test a state on its own

A state reaches its machine through `StateMachine`. To test a state without a grain, give it a substitute machine
with `SetStateMachine`, which is available when the stored state is a class:

```csharp
using Cratis.Choreography.StateMachines;
using NSubstitute;
using Xunit;

public class PublishedSpec
{
    [Fact]
    public async Task PublishingStampsTheTime()
    {
        var state = new Published();
        state.SetStateMachine(Substitute.For<IStateMachine<DocumentState>>());

        var result = await state.OnEnter(new DocumentState());

        Assert.NotEqual(default, result.PublishedAt);
    }
}
```

## Test the grain with Orleans TestKit

To test the transitions and the recorded state together, create the grain on an Orleans TestKit silo, from the
`OrleansTestKit10` package. The silo supplies grain
storage, so no provider has to be configured:

```csharp
using Orleans.TestKit;
using Xunit;

public class DocumentSpec
{
    [Fact]
    public async Task ArchivedDocumentCannotBePublished()
    {
        var silo = new TestKitSilo();
        var document = await silo.CreateGrainAsync<Document>(Guid.NewGuid());

        await document.Archive();
        await document.Publish();

        Assert.IsType<Archived>(await document.GetCurrentState());
    }
}
```

The refused `Publish()` neither throws nor changes anything, which is the behavior the state encodes.
[Testing](../testing/index.md) covers the wider strategy, including in-memory silos and cluster fixtures.

## When it is the wrong fit

- **A status with no behavior attached.** When a grain only reports a status and nothing changes between statuses,
  an enum property is simpler.
- **Transitions that run long work.** `OnEnter` and `OnLeave` run on the grain's turn. Start long work from a
  state and let it report back, for example through a [worker](../workers.md).
- **A state that is not owned by the grain.** When another system is the source of truth, derive the current state
  from it with `ResolveActivationState()` and supplied storage, rather than persisting a competing copy. When that
  system is Chronicle, use an [event-sourced state machine](event-sourced.md).
