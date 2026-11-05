// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import { vi } from 'vitest';
import type { CollaboratorInfo } from '../CollaboratorInfo.js';
import { FakeCollaborationSocket } from './FakeCollaborationSocket.js';

/**
 * The browser's WebSocket stubbed with a fake, for specs of components that join a room. Dispose it after each
 * spec, after the components have been unmounted, so no connection is left in the shared store.
 */
export class a_collaboration_room {
    constructor() {
        vi.stubGlobal('WebSocket', FakeCollaborationSocket);
        FakeCollaborationSocket.reset();
    }

    /** The socket of the connection the components opened. */
    get socket(): FakeCollaborationSocket {
        return FakeCollaborationSocket.latest;
    }

    /**
     * Has the server confirm the join and list who is in the room.
     * @param collaborators Everyone in the room.
     * @param userId The user the local session signed in as.
     */
    deliverRoster(collaborators: CollaboratorInfo[], userId = 'me'): void {
        act(() => this.socket.deliver({ type: 'roster', userId, collaborators }));
    }

    dispose(): void {
        vi.unstubAllGlobals();
    }
}
