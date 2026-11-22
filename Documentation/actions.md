---
title: Actions for state machines and workflows
description: Define what a state machine or a workflow does - execute a command, call a service, append an event, switch state, start a workflow - with fluent actions.
---

State machines and workflows both end up doing things: send a message, append an event, call a service, move another
machine along. An **action** is that thing as an object. `IAction` in `Cratis.Choreography.Actions` has one method, and
the same action runs in a state machine and in a workflow, so what you write for one you can use in the other. You rarely
implement it yourself: you describe the actions fluently with `ActionsBuilder`.

```csharp
public interface IAction
{
    Task<ActionResult> Execute(ActionContext context, CancellationToken cancellationToken);
}
```

An action reports a failure it expects as `ActionResult.Failed(reason)` and a success as `ActionResult.Succeeded(output)`,
where the output is JSON. An exception is an unexpected failure. The `ActionContext` carries the `Identity` of the grain
running the action, the `Services` to resolve dependencies from and the `Input` as JSON.

## Defining actions fluently

`ActionsBuilder` adds actions in the order you write them:

```csharp
actions
    .Execute<PublishDocument>(command => command.Title = "Quarterly report")
    .Call<INotifier>(notifier => notifier.DocumentPublished())
    .Append<DocumentPublished>(@event => @event.Title = "Quarterly report")
    .TransitionTo<Published>();
```

| Method | What it does |
| --- | --- |
| `Execute<TCommand>(populate)` | Creates a Cratis Arc command with its default constructor, lets you populate it, and executes it through Arc's `ICommandPipeline`. It is in `Cratis.Choreography.Arc.Actions`. |
| `Execute<TCommand>((command, context) => ...)` | The same, with the `ActionContext` to populate from, for instance `context.Identity.Key`. |
| `Execute(context => new Command(...))` | For a command that cannot be created with a default constructor, such as a record. |
| `Call<TService>(service => service.Method())` | Resolves `TService` from the grain's services at the time of the call and calls it. The lambda may return a `Task`. |
| `Call(services => services.GetRequiredService<...>().Method())` | Calls code with the grain's `IServiceProvider`, for whatever you need to resolve. The lambda may return a `Task`. |
| `Run(context => ...)` | Runs code that has the `ActionContext`, synchronous or asynchronous. |
| `Append<TEvent>(populate)` | Appends a Chronicle event, populated the same three ways as `Execute`. It is in `Cratis.Choreography.Chronicle.Actions`. |
| `RunProcess(fileName, arguments)` | Runs an operating system process without a shell. |
| `TransitionTo<TState>()` | Switches a state machine to another state. |
| `StartWorkflow(workflow, trigger)` | Starts a workflow and outputs the id of the run. |
| `Add(action)` | Adds any `IAction`. |

`Execute` and `Append` are extension methods, so add `using Cratis.Choreography.Arc.Actions;` or
`using Cratis.Choreography.Chronicle.Actions;` and reference `Cratis.Choreography.Arc` or `Cratis.Choreography.Chronicle`
to get them. The core package depends on neither Cratis Arc nor Cratis Chronicle.

Actions run in the order you add them and the first one that fails stops the rest. Under the fluent methods is a plain
`IAction` for each: `CodeAction`, `ExecuteCommandAction`, `AppendEventAction`, `ProcessAction`, `TransitionStateAction` and
`StartWorkflowAction`, and `CompositeAction` for several in order. Use them directly when you build actions yourself:

```csharp
var announce = new CompositeAction(
    new AppendEventAction(context => new DocumentPublished(context.Identity.Key)),
    new ProcessAction("notify-subscribers", ["--document", "latest"], timeout: TimeSpan.FromSeconds(30)),
    CodeAction.Run(context => metrics.Published()));
```

### Execute a command

`Execute` runs the command through the `ICommandPipeline` resolved from the grain's services, so register Arc's command
handling in the silo. It succeeds when the command succeeds. A command that is not authorized, not valid or that throws
fails the action, and the reason holds what the pipeline reported.

### Append an event

`Append` appends where the grain lives unless you pass a target. For an event-sourced state machine that is its own event
store, namespace and event source id. For a workflow run it is the event store and namespace of the workflow's options, and
the run id. Any other grain has no event store, so pass a target - a `StateMachineKey` - or the action fails.

### Run a process

`RunProcess` and `ProcessAction` run the process without a shell, so the arguments are never interpreted. It succeeds on exit
code 0, fails with the standard error otherwise, and is killed with its child processes when cancelled or when its optional
timeout passes. The output is JSON with the exit code and the standard output. The process is told which grain ran it in the
`CHOREOGRAPHY_GRAIN_KIND`, `CHOREOGRAPHY_GRAIN_TYPE` and `CHOREOGRAPHY_GRAIN_KEY` environment variables.

## Actions in a state machine

A state declares actions for three moments in its life, next to `OnEnter` and `OnLeave`. Define them fluently by overriding
`DefineActions`:

```csharp
public class Publishing : State<DocumentState>
{
    protected override IImmutableList<Type> AllowedTransitions => [typeof(Published), typeof(Failed)];

    protected override void DefineActions(StateActionsBuilder actions) => actions
        .OnEntry(entry => entry.Append<PublishingStarted>(@event => @event.Reason = "requested"))
        .WhileRunning(running => running
            .Execute<PublishDocument>(command => command.Title = "Quarterly report")
            .TransitionTo<Published>())
        .OnExit(exit => exit.Call<IMetrics>(metrics => metrics.PublishingEnded()));
}
```

| Builder method | When the actions execute |
| --- | --- |
| `OnEntry` | As the state machine enters the state, right after `OnEnter` and before `OnAfterEnteringState`. |
| `WhileRunning` | Once the state machine is in the state: it has entered and the state is persisted. |
| `OnExit` | As the state machine leaves the state, right after `OnLeave` and before `OnAfterLeavingState`. |

A transition therefore executes, in order: the exit actions of the state being left, the entry actions of the new
state, the persisting of the state, and the running actions of the new state.

To build the actions yourself, override `EntryActions`, `RunningActions` or `ExitActions` instead. An override replaces what
`DefineActions` defines for that moment.

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

`TransitionTo<TState>()` switches the state machine that runs it. Used while running it makes a state that does its work and
then moves on, as in the example above. The switch is performed once the transition in progress has finished, with the same
guard as any other transition: a state that does not allow the target refuses it, and the action reports
`{"transitioned":false}` instead of failing, as `TransitionTo` does. A transition requested from an exit action is not
supported, as with `OnLeave`.

To switch another state machine, add a `TransitionStateAction` with a resolver that returns an `IStateMachineControl` for it -
a small adapter over that grain's own interface.

## Actions in a workflow

A workflow node of kind `Action` is performed by an `IWorkflowNodePerformer` for its capability. Derive from
`ActionNodePerformer` and define its actions fluently:

```csharp
public class PublishDocumentPerformer(IServiceProvider services) : ActionNodePerformer(services)
{
    public override CapabilityReference Capability => new("documents.publish", 1);

    protected override void Define(ActionsBuilder actions) => actions
        .Execute<PublishDocument>((command, context) => command.Title = context.Input)
        .Append<DocumentPublishedByWorkflow>(@event => @event.Source = "workflow");
}
```

The performer is discovered like any other, or register an instance in the service collection. To supply an `IAction` you
built yourself, override `Action` instead of `Define`. The action sees the node's input as `context.Input` and the workflow
run as its `Identity`. A successful action leaves the node through the `next` outlet - override `SuccessOutlet` to choose
another - with the action's output; a failed one fails the node with its reason, so the node's retry policy applies as for
any performer.

A node is configured by the workflow definition, which is data. That is why no performer that runs a process or a command
named by a node's configuration ships: whoever can edit a definition could then run anything. A performer in your code names
what it runs.

Starting another workflow from a node is the same: use `StartWorkflow` in the performer's `Define`.

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
- [Packages](reference/packages.md) says which package holds what.
