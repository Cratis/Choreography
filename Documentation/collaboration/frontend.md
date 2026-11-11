---
title: Show live collaborators in React
description: Show live collaborator cursors over a pan and zoom canvas and a who-is-here indicator with @cratis/choreography React entry points, themed with CSS variables.
---

The `@cratis/choreography` package ships the browser side of live collaboration: a resilient WebSocket client, a store that shares one connection per page, hooks, the remote cursors, and a who-is-here indicator. It depends on React only for the `/collaboration/react` entry point, and not on a design system, a component library or a CSS framework.

Everyone who joins the same **room** sees each other. A room is any string you choose, for example an application or a document. Within a room, a collaborator is in an **area** (a part of your application, such as `board`) looking at a **surface** (one board or one document in that area). Cursors are drawn only for collaborators in the same area on the same surface.

The package has three entry points.

| Import | Contains |
| --- | --- |
| `@cratis/choreography/collaboration` | The client, the shared store, the types and the pure geometry helpers. No React. |
| `@cratis/choreography/collaboration/react` | The hooks, `CollaboratorCursors`, `CollaborationIndicator` and `CollaborationReconnectIndicator`. |
| `@cratis/choreography/collaboration/collaboration.css` | The styles for the components. |

Import the stylesheet once, where your application starts:

```ts
import '@cratis/choreography/collaboration/collaboration.css';
```

## Connect

By default the client connects to `/ws/collaboration` on the page's own host, joining the room with `room`, `sessionId` and `color` query parameters. Nothing connects until a component, a hook or a call asks for it.

```ts
import { acquireCollaboration, releaseCollaboration } from '@cratis/choreography/collaboration';

const client = acquireCollaboration('project-42');
// ... later
releaseCollaboration(client);
```

`acquireCollaboration` is reference counted. Every caller that asks for the same room gets the same client and the same socket; the socket closes, and the session leaves the room, when the last caller releases it. Asking for a different room closes the connection to the previous one first. The hooks and components do this for you.

To reach another path or another host, pass options when connecting. They apply to the call that creates the connection.

```ts
acquireCollaboration('project-42', { path: '/live/presence' });

acquireCollaboration('project-42', {
    buildUrl: ({ room, sessionId, color }) =>
        `wss://collab.example.com/rooms/${room}?session=${sessionId}&color=${color}`
});
```

The same `options` property is accepted by the hooks and by both components, so whichever mounts first can set it. A `color` option fixes this session's color (a hex value, with or without `#`); otherwise a color is chosen once per browser and remembered. The session id is kept for the life of the tab, and a duplicated tab never shares the id of the tab it was copied from. Where the browser refuses access to `localStorage` or `sessionStorage`, as in a private window, the id and the color last as long as the page.

## Send the pointer position

Positions are in the **content space** of the surface, not in screen pixels, so each client can place a cursor through its own pan and zoom. `useCanvasPresence` does the whole sending side of a canvas: it holds the room's connection while the canvas is mounted, announces the canvas as soon as it opens, maps the pointer into content space, follows the content under a pointer that stands still while the canvas pans or zooms, and leaves the area when the canvas closes.

```tsx
import { useRef } from 'react';
import type { CanvasTransform } from '@cratis/choreography/collaboration';
import { CollaboratorCursors, useCanvasPresence } from '@cratis/choreography/collaboration/react';

const AREA = 'board';

export const Board = ({ room, surfaceId }: { room: string; surfaceId: string }) => {
    const surfaceRef = useRef<HTMLDivElement>(null);
    const transformRef = useRef<CanvasTransform>({ zoom: 1, pan: { x: 0, y: 0 } });
    const { handlePointerMove, handleTransformChange } = useCanvasPresence({ room, area: AREA, surfaceId, transformRef, surfaceRef });

    // Call handleTransformChange from wherever the canvas pans or zooms.
    return (
        <div ref={surfaceRef} style={{ position: 'relative' }} onPointerMove={handlePointerMove}>
            {/* the canvas */}
            <CollaboratorCursors room={room} area={AREA} surfaceId={surfaceId} transformRef={transformRef} />
        </div>
    );
};
```

Without React, the same pieces are available from `@cratis/choreography/collaboration`: `contentPointOf(screen, transform)` maps a point into content space, and `reportCanvasPresence(client, screen, transform, area, surfaceId)` sends it. `sendPresence(x, y, area, surfaceId)` on the client is throttled to about 30 sends a second and always delivers the latest position. After a reconnect the latest position is sent again without another pointer move. `leaveArea()` sends a presence with no area, which hides the cursor and lists the session without an area.

## Draw the cursors over a canvas

`CollaboratorCursors` is an overlay that fills its nearest positioned ancestor. Place it over your canvas and give it the room, the area and the surface to show.

```tsx
import { useRef } from 'react';
import { CollaboratorCursors } from '@cratis/choreography/collaboration/react';

export const Board = ({ projectId, boardId }: { projectId: string; boardId: string }) => {
    // The live pan and zoom. A ref, so panning never re-renders the board or the cursors.
    const transformRef = useRef({ zoom: 1, pan: { x: 0, y: 0 } });
    const share = useShareCursor(boardId, transformRef);

    return (
        <div style={{ position: 'relative', overflow: 'hidden' }} onPointerMove={share}>
            <MyPanAndZoomCanvas transformRef={transformRef} />
            <CollaboratorCursors
                room={projectId}
                area='board'
                surfaceId={boardId}
                transformRef={transformRef}
                transformActivity={myCanvasTransformActivity} />
        </div>
    );
};
```

`transformRef` is read inside the cursors' own animation frame, so it needs only a `current` of `{ zoom, pan: { x, y } }`, with `screen = content * zoom + pan`. `transformActivity` is optional: an object with `subscribe(listener): () => void` that your canvas calls whenever it applies a new pan or zoom. Without it, a pan or zoom made while nobody else moves is picked up on the next presence update; with it, cursors follow the pan at once.

How the cursors behave:

- A collaborator who is on screen has an ordinary mouse pointer with a pill carrying their avatar and name in its tail.
- Positions are drawn about 120 ms in the past, along a Catmull-Rom curve through the recent samples, so the roughly 30 ms presence updates read as one smooth motion.
- A collaborator outside the visible area is not dropped. Their pill parks at the nearest edge and the arrow orbits it, pointing the way you would pan to reach them.
- A collaborator who has gone idle is dimmed.
- Animation frames run only while a cursor still has motion to render; an idle surface costs nothing.

### Draw the cursors as layers, or with real avatars

| Property | Use |
| --- | --- |
| `layerProps` | Attributes put on every cursor's arrow element and pill element, for example to mark them as layers for a compositing or glass effect. The arrow and the pill are siblings so each can be its own layer. |
| `renderAvatar` | `(subject, size) => ReactNode`, drawn in the pill in place of the default initials circle. `subject` has `userId`, `displayName`, `color` and `hasAvatar`, so you can ask for an image only when there is one. |
| `className` | An extra class name for the overlay. |

```tsx
<CollaboratorCursors
    room={projectId}
    area='board'
    surfaceId={boardId}
    transformRef={transformRef}
    layerProps={{ 'data-layer': 'over' }}
    renderAvatar={(person, size) =>
        person.hasAvatar
            ? <img src={`/avatars/${person.userId}`} width={size} height={size} alt='' />
            : <MyInitials name={person.displayName} size={size} />} />
```

## Show who is here

`CollaborationIndicator` is a button with a count badge that opens a panel listing the people in the room. It lists one entry per person with the area they are in and how many tabs they have open, and says "Only you" when nobody else is here. Your own other tabs are never listed as collaborators; the panel mentions them at the foot instead. The panel closes on an outside press or Escape.

```tsx
import { CollaborationIndicator } from '@cratis/choreography/collaboration/react';

<CollaborationIndicator
    room={projectId}
    areaLabels={{ board: 'Board', notes: 'Notes' }} />
```

An area without an entry in `areaLabels` is shown as it is. While the connection has been down for 30 seconds, the button turns red and struck through, its tooltip says why, and a status message is announced to assistive technology; shorter outages stay quiet.

| Property | Use |
| --- | --- |
| `areaLabels` | Display names for areas, keyed by area. |
| `labels` | Overrides for any of the strings: `title`, `onlyYou`, `idle`, `tabs`, `ownTabs`, `reconnecting` and `collaborators`. The defaults are English. `tabs`, `ownTabs` and `collaborators` receive their number as `{count}`. |
| `renderAvatar` | The same function as for the cursors, called with the grouped person and a size of 28. |
| `renderTrigger` | Draws the button yourself, so it can be your toolbar button. It receives `{ toggle, isOpen, title, connectionState, count }`. The indicator keeps the container, the badge and the panel. |
| `panelAlignment` | `end` (the default) lines the panel up with the right edge of the trigger, `start` with the left. |
| `className` | An extra class name for the container. |

```tsx
<CollaborationIndicator
    room={projectId}
    labels={{ title: 'Aktive samarbeidspartnere', onlyYou: 'Bare deg' }}
    renderTrigger={({ toggle, title, count }) => (
        <ToolbarButton icon='users' title={title} badge={count} onClick={toggle} />
    )} />
```

The panel is positioned under the indicator's container rather than moved to the end of the page, so it inherits your theme variables, and an ancestor that clips its overflow clips the panel too. Give the indicator a container that does not.

When you draw the trigger yourself, the container carries the `collaboration-indicator--reconnecting` class while reconnecting. Use it to style your own icon.

## Read the state yourself

The hooks take the room and share the same connection as the components. They re-render only the component that uses them, never the rest of the application.

```tsx
import { useCollaborators, useOwnOtherTabs, useCollaborationConnectionState } from '@cratis/choreography/collaboration/react';

const collaborators = useCollaborators(projectId);       // other people's sessions
const ownOtherTabs = useOwnOtherTabs(projectId);         // the signed-in user's other tabs
const connection = useCollaborationConnectionState(projectId);
```

`useCollaborators(room)` returns one `CollaboratorInfo` per session, with `sessionId`, `userId`, `displayName`, `color`, `x`, `y`, `area`, `surfaceId`, `isIdle` and `hasAvatar`. The array keeps its identity until the roster really changes. Pass `undefined` as the room while it is not known yet. `groupCollaborators` from `@cratis/choreography/collaboration` turns sessions into one entry per person.

## Theme

The stylesheet reads CSS custom properties with the defaults below. Set them on `:root`, or on any ancestor of the components, to match your design. The panel and badge have a light default; override the variables for a dark theme.

| Property | Default | Controls |
| --- | --- | --- |
| `--collaboration-cursor-z-index` | `10` | Stacking of the cursors overlay. |
| `--collaboration-cursor-font-family` | `inherit` | Font of the name pill. |
| `--collaboration-cursor-text` | `#fff` | Text color of the name pill. |
| `--collaboration-cursor-outline` | `#fff` | Outline of the pointer arrow. |
| `--collaboration-cursor-idle-opacity` | `0.5` | Opacity of an idle collaborator's cursor. |
| `--collaboration-surface` | `#fff` | Panel background. |
| `--collaboration-text` | `#1f2937` | Primary text. |
| `--collaboration-text-secondary` | `#6b7280` | Secondary text. |
| `--collaboration-border` | `#e5e7eb` | Panel border. |
| `--collaboration-hover` | `rgba(0, 0, 0, 0.06)` | Hover background of the default button. |
| `--collaboration-accent` | `#2563eb` | Count badge background and focus ring. |
| `--collaboration-accent-text` | `#fff` | Count badge text. |
| `--collaboration-danger` | `#ef4444` | Reconnecting color. |
| `--collaboration-radius` | `8px` | Radius of the panel and the default button. |
| `--collaboration-shadow` | a soft drop shadow | Panel shadow. |

```css
:root {
    --collaboration-surface: #1e1e24;
    --collaboration-text: #f3f4f6;
    --collaboration-text-secondary: #9ca3af;
    --collaboration-border: #33333b;
    --collaboration-accent: #7c3aed;
}
```

A collaborator's own color is not themed: it identifies the person, so it is set inline from the `color` the backend reports.

## The wire protocol

A backend that is not Choreography's own can serve the client. It accepts a WebSocket at the configured path with the query parameters `room`, `sessionId` and `color` (hex, no `#`).

| Direction | Message |
| --- | --- |
| Server to client | `{ "type": "roster", "userId": "...", "collaborators": [...] }`, sent on every join and confirming it. |
| Server to client | `{ "type": "joined", "collaborator": {...} }` and `{ "type": "left", "sessionId": "..." }`. |
| Server to client | `{ "type": "presence", "sessionId": "...", "x": 0, "y": 0, "area": "...", "surfaceId": "..." }` and `{ "type": "idle", "sessionId": "...", "isIdle": true }`. |
| Server to client | `{ "type": "ping" }`, about every 20 seconds. |
| Client to server | `{ "type": "presence", "x": 0, "y": 0, "area": "...", "surfaceId": "..." }` and `{ "type": "pong" }`. |

A collaborator is `{ sessionId, userId, displayName, color, x, y, area, surfaceId, isIdle, hasAvatar }`. The client treats the roster as the only proof of a join: a socket that opens but never receives a roster is not connected. It tears down a connection that hears nothing for twice the ping interval, reconnects with a growing delay, and shows nothing about it until the outage has lasted 30 seconds. A repeated position does not wake an idle collaborator; only moving does.
