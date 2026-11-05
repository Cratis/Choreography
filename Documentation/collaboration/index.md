---
title: Live collaboration presence
description: Show who else is on a page and where their cursors are with a WebSocket presence hub in Cratis Choreography, with a shared roster across replicas on Orleans.
---

You build a board, a document or a canvas that several people open at once. Without presence, nobody knows anyone else is
there, and two people edit the same thing without noticing. Cursors that follow the mouse and a list of who is here fix that,
but they are awkward to build: every browser tab is a connection that can vanish without a goodbye, and as soon as you run
two replicas, people on different replicas stop seeing each other.

Cratis Choreography ships a **presence hub** for this. Browsers connect over a WebSocket to a **room**, the hub keeps a roster
of who is in it, and it relays cursor positions and idle state to everyone else in the room. Presence is ephemeral: a
disconnect is a leave, and nothing is stored. The hub is part of `Cratis.Choreography`, so there is no extra package to add.

## What you get

- A WebSocket endpoint that you map with one call and that asks your code who is connecting.
- A roster per room, with display name, color, cursor position, the area and surface each person is looking at, and idle state.
- Ghost removal: a half-open connection is dropped after 75 seconds of silence, and a replica that is killed has its
  sessions expire from the shared roster.
- An optional Orleans backplane that shares the roster across replicas, so the hub is correct at any number of replicas.
- A React client in `@cratis/choreography`. See [Show live collaborators in React](frontend.md).

## The pieces

| You provide | The library provides |
| --- | --- |
| An `ICollaborationParticipantResolver` that decides who connects and which group they join | The WebSocket endpoint, the hub and the wire protocol |
| A decision whether to run on one replica or many | The local backplane, and an Orleans backplane you opt into |

The library knows nothing about your users or your tenants. Your resolver supplies both. That keeps the identity and
tenancy decisions in one place that you own. See [Set up the backend](backend.md).

## Read on

| You want to | Read |
| --- | --- |
| Add the hub to an ASP.NET Core application | [Set up the backend](backend.md) |
| Write your own client, or debug one | [The wire protocol](wire-protocol.md) |
| Run more than one replica | [Run on more than one replica](multiple-replicas.md) |
| Draw the cursors in a React application | [Show live collaborators in React](frontend.md) |

## When this is the wrong fit

Presence is best-effort and lives in memory. If you need a durable record of who did what, append that as events
or operations instead. If you need to merge concurrent edits to the same content, you need a collaborative editing
engine; presence only shows who is there.
