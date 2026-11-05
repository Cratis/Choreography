// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CollaboratorInfo } from './CollaboratorInfo.js';

/**
 * A message the backend sends over the collaboration WebSocket.
 */
export interface ServerMessage {
    /** What the message is. */
    type: 'roster' | 'joined' | 'left' | 'presence' | 'idle' | 'ping';

    /** On a roster: the user this session signed in as, so the client can tell its own user's other tabs apart. */
    userId?: string;

    /** On a roster: everyone in the room. */
    collaborators?: CollaboratorInfo[];

    /** On a join: the collaborator that joined. */
    collaborator?: CollaboratorInfo;

    /** On a leave, presence or idle: the session concerned. */
    sessionId?: string;

    /** On presence: the cursor's horizontal position. */
    x?: number;

    /** On presence: the cursor's vertical position. */
    y?: number;

    /** On presence: the area the session is in. */
    area?: string;

    /** On presence: the surface the session is looking at. */
    surfaceId?: string;

    /** On idle: whether the session is idle. */
    isIdle?: boolean;
}
