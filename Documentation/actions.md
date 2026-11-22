---
title: Actions for state machines and workflows
description: Run code, execute a command, append an event, switch state or start a workflow from a state machine or a workflow, alone or several in a composite action.
---

State machines and workflows both end up doing things: send a message, append an event, call a tool, move another
machine along. An **action** is that thing as an object. `IAction` in `Cratis.Choreography.Actions` has one method, and
the same action runs in a state machine and in a workflow, so what you write for one you can use in the other.

```csharp
public interface IAction
{
    Task<ActionResult> Execute(ActionContext context, CancellationToken cancellationToken);
}
```

An action reports a failure it expects as `ActionResult.Failed(reason)` and a success as `ActionResult.Succeeded(output)`,
where the output is JSON. An exception is an unexpected failure. The `ActionContext` carries the `Identity` of the grain
running the action, the `Services` to resolve dependencies from and the `Input` as JSON.

## The actions that ship

| Action | What it does |
| --- | --- |
| `CodeAction` | Runs your code. `CodeAction.Run(context => ...)`, synchronous or asynchronous, succeeds unless the code throws; the constructor takes code that returns its own `ActionResult`. |
| `CommandAction` | Executes a command - an operating system process - without a shell. It succeeds on exit code 0, fails with the standard error otherwise, and kills the process tree when cancelled or when its optional timeout passes. |
| `AppendEventAction` | Appends a Chronicle event. It is in `Cratis.Choreography.Chronicle.Actions`. |
| `TransitionStateAction` | Switches a state machine to another state. |
| `StartWorkflowAction` | Starts a workflow and outputs the id of the run. |
| `CompositeAction` | Runs several actions in order. The first failure stops the rest and fails the composite. It is itself an action, so composites nest. |

```csharp
var announce = new CompositeAction(
    new AppendEventAction(context => new DocumentPublished(context.Identity.Key)),
    new CommandAction("notify-subscribers", ["--document", "latest"], timeout: TimeSpan.FromSeconds(30)),
    CodeAction.Run(context => metrics.Published()));
```

A command is told which grain ran it in the `CHOREOGRAPHY_GRAIN_KIND`, `CHOREOGRAPHY_GRAIN_TYPE` and
`CHOREOGRAPHY_GRAIN_KEY` environment variables.

`AppendEventAction` appends where the grain lives unless you pass a target. For an event-sourced state machine that is its own
event store, namespace and event source id. For a workflow run it is the event store and namespace of the workflow's
options, and the run id. Any other grain has no event store, so pass a target - a `StateMachineKey` - or the action fails.

## Actions in a state machine

A state declares actions for three moments in its life, next to `OnEnter` and `OnLeave`:

```csharp
public class Publishing : State<DocumentState>
{
    protected override IImmutableList<Type> AllowedTransitions => [typeof(Published), typeof(Failed)];

    protected override IEnumerable<IAction> EntryActions => [new AppendEventAction(_ => new PublishingStarted())];

    protected override IEnumerable<IAction> RunningActions => [new CommandAction("publish-document")];

    protected override IEnumerable<IAction> ExitActions => [CodeAction.Run(_ => metrics.PublishingEnded())];
}
```

| Property | When the actions execute |
| --- | --- |
| `EntryActions` | As the state machine enters the state, right after `OnEnter` and before `OnAfterEnteringState`. |
| `RunningActions` | Once the state machine is in the state: it has entered and the state is persisted. |
| `ExitActions` | As the state machine leaves the state, right after `OnLeave` and before `OnAfterLeavingState`. |

A transition therefore executes, in order: the exit actions of the state being left, the entry actions of the new
state, the persisting of the state, and the running actions of the new state.

Two cases are deliberately different from `OnEnter` and `OnLeave`, because actions have effects in the world and `OnEnter`
only rebuilds what is in memory:

- **Reactivation resumes; it does not re-enter.** When the grain activates in a state it was already in, only the running
  actions execute - the entry actions ran when the state was first entered. The first state of a machine that has never
  persisted one is a first entry, so its entry actions run. A machine whose stored state is not a `StateMachineState`
  cannot tell and treats every activation as a resume.
- **Deactivation does not exit.** Exit actions execute when the machine transitions out of a state, not when the grain
  deactivates, although `OnLeave` still runs on deactivation.

An action that fails, as a failed result or an exception, fails the transition: a failed result throws
`StateActionFailed`, which says the state and the phase.

### Switching state from an action

`TransitionStateAction.To<TState>()` switches the state machine that runs it. Used as a running action it makes a state that
does its work and then moves on:

```csharp
protected override IEnumerable<IAction> RunningActions =>
[
    new CommandAction("publish-document"),
    TransitionStateAction.To<Published>()
];
```

The switch is performed once the transition in progress has finished, with the same guard as any other transition: a state
that does not allow the target refuses it, and the action reports `{"transitioned":false}` instead of failing, as
`TransitionTo` does. To switch another state machine, give the action a resolver that returns an `IStateMachineControl` for
it - a small adapter over that grain's own interface. A transition requested from an exit action is not supported, as with `OnLeave`.

## Actions in a workflow

A workflow node of kind `Action` is performed by an `IWorkflowNodePerformer` for its capability. Derive from
`ActionNodePerformer` to perform it with an action:

```csharp
public class PublishDocumentPerformer(IServiceProvider services) : ActionNodePerformer(services)
{
    public override CapabilityReference Capability => new("documents.publish", 1);

    protected override IAction Action { get; } = new CompositeAction(
        new CommandAction("publish-document"),
        new AppendEventAction(_ => new DocumentPublishedByWorkflow()));
}
```

The performer is discovered like any other, or register an instance in the service collection. The action sees the node's
input as `context.Input` and the workflow run as its `Identity`. A successful action leaves the node through the `next`
outlet - override `SuccessOutlet` to choose another - with the action's output; a failed one fails the node with its
reason, so the node's retry policy applies as for any performer.

A node is configured by the workflow definition, which is data. That is why no performer that executes a command named by a
node's configuration ships: whoever can edit a definition could then run any command. A performer in your code names its
command.

Starting another workflow from a node is the same: use a `StartWorkflowAction` in the performer's action.

## Identity

A state machine, a workflow and a workflow run are grains, and each says which one it is. `StateMachine<TStoredState>`,
`WorkflowGrain` and `WorkflowRunGrain` implement `IIdentifiable`, whose `Identity` is a `GrainIdentity`: the `GrainKind`
(`StateMachine`, `Workflow` or `WorkflowRun`), the Orleans grain type and the key as text. It is derived from the grain's
Orleans id, so it needs no storage and no new grain method. The same identity is on the `ActionContext`, so an action knows
which grain it is running for.

## Where to go next

- [State machines](state-machines/index.md) cover states, transitions and what a transition does.
- [Workflows](workflows/index.md) cover graphs, runs and performers.
- [Event-sourced state machines](state-machines/event-sourced.md) drive the same states from Chronicle events.
