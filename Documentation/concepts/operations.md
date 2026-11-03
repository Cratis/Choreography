---
title: Operation history and undo/redo
description: Record logical operations with the events they produced and undo or redo them as new operations driven by explicit, current-state rules.
---

An **operation** is one logical user action, independent of how many events it emits: renaming a module is one operation and one event, deleting a hierarchy is one operation and many. The **operation history** records each operation with its scope, actor, kind, lifecycle and the event sequence numbers it produced. The **undo/redo engine** builds on it to reverse and reissue an actor's operations as new operations, so the original history is never rewritten.

Both live in `Cratis.Choreography` and `Cratis.Choreography.Storage` (namespace `Cratis.Choreography.Operations`). They depend on neither Orleans grains, Chronicle nor any application type.

## Record an operation

```csharp
var entry = await history.Record("model-1", "alice", "RenameModule", async recording =>
{
    var sequenceNumbers = await AppendEvents(recording.Id);
    await recording.Associate(sequenceNumbers);   // zero, one or many events, any number of calls
});
```

- `OperationId` is stable and generated. `OperationScope`, `OperationActor` and `OperationKind` are opaque strings you map your own identity onto.
- Event sequence numbers are plain `ulong` **references**. They never prove that reversing something is safe.
- A throw inside the work records the operation as `Failed` (keeping any references already made) and rethrows.
- `IOperationHistory` also offers `GetFor(scope, actor?)`, in order, and `GetLatest(scope, actor)`.
- Entries are only appended; a later change touches lifecycle fields, never identity or the events.

Register storage and the history with `AddCratisChoreographyInMemoryOperationHistory()` and `AddCratisChoreographyOperations()`. Only an in-memory storage ships; implement `IOperationHistoryStorage` for a durable one.

## Define undo and redo

Reversal is explicit. The definition supplies the inverse command; the engine never infers one from events.

```csharp
definitions.Define<RenameModule>()
    .UndoWith(ctx => new RenameModule(ctx.Command.Module, ctx.GetMemento<string>()!))
    .CanUndoWhen(ctx => modules.Get(ctx.Command.Module).Name == ctx.Command.NewName,
        "The module was renamed again after this operation")
    .CanRedoWhen(ctx => modules.Get(ctx.Command.Module).Name == ctx.GetMemento<string>(),
        "The module changed since the undo");
```

- `CanUndoWhen` and `CanRedoWhen` are evaluated against **current state** (read through `ctx.Services`), so an unrelated later change does not block an undo and a conflicting one does.
- `RedoWith` is optional; without it redo reissues the original command after validation. It never replays event payloads.
- `…Async` variants take asynchronous delegates.
- The host can return a *memento* (for example the previous name) from its executor; it comes back as `ctx.GetMemento<T>()`.

## Run it

The host implements one interface, which keeps the engine independent of transport and Chronicle:

```csharp
public class HostExecutor : IOperationExecutor
{
    public async Task<OperationExecutionResult> Execute(object command, OperationExecutionContext context)
    {
        var sequenceNumbers = await Dispatch(command, context.OperationId);
        return OperationExecutionResult.Success(sequenceNumbers);
    }
}
```

`IOperationEngine` is the facade:

| Call | Does |
| --- | --- |
| `Execute(scope, actor, command)` | Performs and records a defined operation; clears the actor's redo stack |
| `Undo(scope, actor)` | Reverses the actor's latest active operation in the scope |
| `Redo(scope, actor)` | Reissues the actor's most recently undone operation |
| `GetHistory(scope, actor)` | The actor's history with `CanUndo`/`CanRedo` and `BlockedReason` |

Each returns an `OperationOutcome`: `Succeeded`, `Blocked` (with a reason), `NothingToUndo`, `NothingToRedo` or `Failed`.

## Rules the engine follows

- **The latest operation is never skipped.** If it is blocked, the outcome says why and an older operation is left alone.
- **Reversal is new activity.** An undo is a new `Undo` operation targeting the original, with its own events; the original becomes `Undone`. A redo is a new `Redo` operation.
- **Multi-event operations are one unit.** One inverse command, one undo operation, all its events associated with it.
- **A new operation clears the actor's redo stack** (the undone operations become `Discarded`). Other actors' operations do not, but may make redo ineligible through `CanRedoWhen`.
- A serialized intent of each forward command is stored with its operation. Commands, mementos and outcomes serialize with System.Text.Json, so they can cross to a client as plain records.

For the frontend, see [Use operation history and undo/redo in React](../how-to/use-operation-history-in-react.md).
