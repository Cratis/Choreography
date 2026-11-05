---
title: The collaboration wire protocol
description: Reference for the WebSocket query parameters, the JSON messages the presence hub sends and receives, close codes, and the timing the hub enforces.
---

The hub speaks JSON over a WebSocket. Property names are camelCase. The React client in `@cratis/choreography` implements this
protocol; read this page if you write another client or debug one.

## Connecting

The default path is `/ws/collaboration`. See [Set up the backend](backend.md) to change it.

| Query parameter | Required | Meaning |
| --- | --- | --- |
| `room` | Yes | The room to join. Your `ICollaborationParticipantResolver` decides what it means and whether the caller may join it. |
| `sessionId` | Yes | Identifies this browser tab. Reconnecting with the same value replaces the previous connection of that session. |
| `color` | No | The cursor color as hex without the leading `#`, for example `ff00ff`. |

A request that is not a WebSocket upgrade, or lacks `room` or `sessionId`, is answered with `400`. A caller the resolver
refuses is answered with `401`.

## Messages from the server

| `type` | Properties | Sent when |
| --- | --- | --- |
| `roster` | `userId`, `collaborators` | Once, right after joining. `userId` is the caller's own user id; `collaborators` lists the other sessions in the room. |
| `joined` | `collaborator` | A session joins the room. |
| `left` | `sessionId` | A session leaves or expires. |
| `presence` | `sessionId`, `x`, `y`, `area`, `surfaceId` | A session moves, or changes area or surface. |
| `idle` | `sessionId`, `isIdle` | A session becomes idle or active again. |
| `ping` | none | Every 20 seconds. |

Messages about a session are never sent to that session itself.

A `collaborator` looks like this:

```json
{
  "sessionId": "tab-1",
  "userId": "42",
  "displayName": "Jane Doe",
  "color": "ff00ff",
  "x": 120,
  "y": 340,
  "area": "board",
  "surfaceId": "board-7",
  "isIdle": false,
  "hasAvatar": true
}
```

| Property | Meaning |
| --- | --- |
| `userId` | The user id string from your resolver. |
| `area` | A part of your application, chosen by the client, such as `board`. |
| `surfaceId` | One surface inside the area, such as a single board or document. |
| `hasAvatar` | Whether the user has an avatar image the others can request. |

## Messages from the client

| `type` | Properties | Meaning |
| --- | --- | --- |
| `presence` | `x`, `y`, `area`, `surfaceId` | The cursor position and where the session is. Send it whenever the cursor moves. |
| `pong` | none | The answer to a `ping`. |

Other message types and frames that are not valid JSON are ignored without ending the session.

## Timing

| Behavior | Value |
| --- | --- |
| Ping interval | 20 seconds |
| Idle after no cursor movement | 30 seconds |
| Dropped after no message of any kind | 75 seconds |

Sending `presence` or `pong` counts as a sign of life. A client that answers every ping stays connected, and a laptop that went
to sleep is removed instead of lingering as a ghost.

## Close codes

When the server closes a connection because it cannot keep the session consistent, it closes with status `1001`
(`EndpointUnavailable`) and the reason `Collaboration is reconnecting`. That means the client should reconnect and join again, which
rebuilds its presence. The cases are: the cluster stayed unavailable for the retry budget, the session joined on another
replica, the shared roster was lost, or the connection was silent for too long. A normal close is `1000`.
