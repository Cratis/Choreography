---
title: Run collaboration on more than one replica
description: Share the presence roster across replicas with the Orleans backplane of Cratis Choreography, and how it recovers when a replica or silo disappears.
---

With one replica the hub holds everything in memory. With two, a person on replica A would never see a person on replica B,
because each replica only knows its own sockets. The Orleans backplane fixes that: it keeps one authoritative roster per room
in a grain, and relays events between replicas.

## Prerequisites

- [The backend set up](backend.md) on one replica.
- An Orleans silo, or a cluster client, in every replica, so that `IGrainFactory` is available. See [Hosting](../hosting/index.md).

## Turn it on

Call `AddCratisCollaborationOrleans` after `AddCratisCollaboration`, only for the deployments that run in a cluster.

```csharp
builder.Services.AddCratisCollaboration<DocumentParticipantResolver>();

if (builder.Configuration.GetValue("Orleans:Enabled", false))
{
    builder.Services.AddCratisCollaborationOrleans();
}
```

It replaces the local backplane with `OrleansCollaborationBackplane` and registers it as a hosted service. Nothing else
changes, and you can leave it off in development, where one replica is all you run.

## How it works

Each replica still sends events to its own sockets directly. That is the fast path, and it is why cursors stay smooth. The
backplane only handles the part between replicas.

- A room has a grain, `ICollaborationRoomGrain`, keyed by the group key your resolver returned. It holds the roster and has no
  persisted state: it deactivates when the roster empties.
- Each replica registers an observer with the grains of the rooms it has sockets in. The grain pushes joins, leaves, cursor moves
  and idle changes to every other replica, which hand them to their local sockets.
- A joining client's roster is its local sockets plus the sessions the grain reports for other replicas.

## What happens when something fails

The hub is built so a failure ends in a reconnect, never in a wrong roster.

| Situation | What the hub does |
| --- | --- |
| A silo leaves while a call is in flight | Retries with a short backoff for up to 10 seconds, resolving the grain again each time. |
| The cluster stays unavailable for the whole budget | Closes the socket with `1001`. The client reconnects and rejoins. |
| The grain moved to a new activation and lost its roster | Asks every local socket to reconnect so the roster is rebuilt from the live connections. |
| A replica is killed or rolled out | It stops vouching for its sessions every 15 seconds, and after 60 seconds the grain removes them and tells the others. |
| The same session reconnects on another replica | The old replica drops its stale connection quietly, and a late leave from the old connection does not remove the new one. |
| A replica still runs an older version that cannot vouch | Its sessions are never expired by vouching, only by their own leave, so a rolling upgrade does not drop them. |

Failures that are not transient, such as a serialization error, are not retried: they surface instead of being hidden as an outage.

## Compatibility between releases

The grain interfaces of this feature follow the same rule as the rest of `Cratis.Choreography`: within a major version they only
grow, so replicas of two releases can share one cluster during a rolling upgrade. See [Upgrading and compatibility](../upgrading/index.md).
The `Collaborator` record is a serialized contract as well, and only gains members.

## When it does not work

If people on different replicas do not see each other, check that `AddCratisCollaborationOrleans` is called in every replica and
that the group key your resolver returns is the same for the people who should meet.
