---
title: Use operation history and undo/redo in React
description: Read an actor's operation history, and undo and redo from a React application with the @cratis/choreography package.
---

The `@cratis/choreography` package is the frontend companion to the [operation history and undo/redo engine](../concepts/operations.md). It has no dependency on a particular application: you give it a transport, and build whatever history or undo controls you need on top.

The backend is authoritative. The history shows which operation an undo would reverse and whether it is currently safe, but undo and redo are still validated on the server when they are requested, and a conflicting change comes back as a blocked outcome rather than an error.

## Map the engine to HTTP

The package ships an HTTP transport for a host that exposes `IOperationEngine` on three routes. Choreography does not map them for you, because authorization and the actor's identity are the host's to decide.

| Route | Calls | Returns |
| --- | --- | --- |
| `GET {baseUrl}/history?scope=&actor=` | `GetHistory(scope, actor)` | the `OperationHistoryItem` list, oldest first |
| `POST {baseUrl}/undo` with `{ scope, actor }` | `Undo(scope, actor)` | an `OperationOutcome` |
| `POST {baseUrl}/redo` with `{ scope, actor }` | `Redo(scope, actor)` | an `OperationOutcome` |

Write JSON with camelCase names and enums as strings. A `Blocked`, `NothingToUndo` or `NothingToRedo` outcome is a normal `200` answer. Take the actor from the authenticated user rather than trusting the request body.

## Create the client

```ts
import { OperationHistoryClient, createHttpTransport } from '@cratis/choreography';

const client = new OperationHistoryClient(createHttpTransport({
    baseUrl: '/api/operations',
    headers: () => ({ Authorization: `Bearer ${getToken()}` })
}));
```

Any object that implements `OperationHistoryTransport` works in place of the HTTP one. If your application has a push channel, implement the optional `subscribe` method and the hooks re-read when it fires.

## Show the history and undo

```tsx
import { useOperationHistory, useRedo, useUndo } from '@cratis/choreography/react';

export const HistoryControls = ({ scope, actor }: { scope: string; actor: string }) => {
    const history = useOperationHistory({ client, scope, actor });
    const undo = useUndo({ client, scope, actor });
    const redo = useRedo({ client, scope, actor });

    return (
        <>
            <button disabled={!history.canUndo || undo.isPending} onClick={undo.execute}>Undo</button>
            <button disabled={!history.canRedo || redo.isPending} onClick={redo.execute}>Redo</button>
            {history.nextUndo?.blockedReason && <p>{history.nextUndo.blockedReason}</p>}
            {undo.outcome?.kind === 'Blocked' && <p>{undo.outcome.reason}</p>}
            {undo.error && <p>Undo could not be completed: {undo.error.message}</p>}
        </>
    );
};
```

- `useOperationHistory` returns the items, `isLoading`, `error`, and the `nextUndo` and `nextRedo` items with their blocked reasons. A failed refresh keeps the previous items. A response that arrives after the scope or actor changed is ignored.
- `useUndo` and `useRedo` return `execute`, `isPending`, `outcome`, `error` and `reset`. A blocked request is an `outcome`; `error` is only for a request that could not be completed. A request already in flight is not sent twice.
- After an undo or redo through the same client, mounted history hooks for that scope and actor re-read. After a forward operation made through your own commands, call `client.notifyChanged(scope, actor)`, or pass `pollIntervalInMilliseconds` to `useOperationHistory`.

Operations are the backend's grouping, not something to rebuild from events: `eventSequenceNumbers` on an item are references for display and linking only.
