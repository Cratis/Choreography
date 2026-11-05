---
title: Set up the collaboration backend
description: Add the Cratis Choreography presence hub to an ASP.NET Core application, write the participant resolver that supplies identity and tenancy, and map the WebSocket endpoint.
---

This page takes an ASP.NET Core application from nothing to a working presence endpoint. By the end, a browser can connect to
`/ws/collaboration?room=...&sessionId=...` and receive the roster of the room.

## Prerequisites

- An ASP.NET Core application that references `Cratis.Choreography`.
- Authentication that has already identified the caller by the time the WebSocket request reaches the endpoint.

## Write the participant resolver

The hub does not know who your users are. You implement `ICollaborationParticipantResolver` to tell it. It receives the
`HttpContext` of the upgrade request and the room the client asked for, and returns a `CollaborationParticipant`, or
`null` to refuse the connection.

```csharp
using Cratis.Choreography.Collaboration;

public class DocumentParticipantResolver(ICurrentUser currentUser, ICurrentTenant currentTenant)
    : ICollaborationParticipantResolver
{
    public async Task<CollaborationParticipant?> Resolve(HttpContext context, string room)
    {
        var user = await currentUser.Get();
        if (user is null)
        {
            return null;
        }

        return new CollaborationParticipant(
            GroupKey: $"{currentTenant.Id}|{room}",
            UserId: user.Id.ToString(),
            DisplayName: user.FullName,
            HasAvatar: user.HasAvatar);
    }
}
```

`ICurrentUser` and `ICurrentTenant` stand for whatever your application already uses.

The `GroupKey` is what decides who sees whom. Sockets with the same group key see each other. Put everything presence must not
leak across into it. The sample prefixes the tenant, so two organizations that both have a room called `board-1` never share a
roster. Because the client chooses the room, never use the room name alone as the group key in a multi-tenant application.

Returning `null` answers `401 Unauthorized`. Also return `null` when the user is allowed in the application but not in that
particular room.

## Register and map

```csharp
using Cratis.Choreography.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCratisCollaboration<DocumentParticipantResolver>();

var app = builder.Build();
app.UseAuthentication();
app.UseWebSockets();
app.MapCratisCollaboration();
app.Run();
```

`AddCratisCollaboration` registers the hub, your resolver and the single-replica backplane. The resolver is registered as
scoped, because it is resolved for each upgrade request and typically depends on the current user. `MapCratisCollaboration`
maps the endpoint on `/ws/collaboration`.

The application must call `UseWebSockets()` itself, and your authentication middleware must run before the endpoint, so that
your resolver can read the caller.

To change the path, configure the options. The browser client connects to the same path, so change it on both sides.

```csharp
builder.Services.AddCratisCollaboration<DocumentParticipantResolver>(options => options.Path = "/live/presence");
```

## What happens on a connection

1. The endpoint refuses a request that is not a WebSocket upgrade, or that lacks `room` or `sessionId`, with `400 Bad Request`.
2. It calls your resolver. A `null` result is a `401 Unauthorized`.
3. It accepts the socket and hands it to the hub together with the group key, user, name and color from the request.
4. The hub adds the session to its roster, tells the others, and sends the new session the roster. When the socket closes,
   the hub tells the others that the session left.

Every browser tab is its own **session**, identified by the `sessionId` the client sends. One user with three tabs appears
as three sessions with the same `userId`.

## Verify it

Connect with any WebSocket client and send nothing. The first message you receive is the roster:

```json
{ "type": "roster", "userId": "42", "collaborators": [] }
```

The `userId` is the one your resolver returned. If you get `401`, check what your resolver returns for that request. See
[The wire protocol](wire-protocol.md) for every message.

## Next step

If you run more than one replica, add the Orleans backplane. See [Run on more than one replica](multiple-replicas.md).
