// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';
import { FakeCollaborationSocket } from '../../testing/FakeCollaborationSocket.js';

/**
 * Stubs the browser's WebSocket for the specs of the collaboration store, and fails loudly if a spec leaves
 * a connection open - the store is module-level state and would leak into the next spec.
 */
export class a_collaboration_store {
    constructor() {
        vi.stubGlobal('WebSocket', FakeCollaborationSocket);
        FakeCollaborationSocket.reset();
    }

    /** The sockets that were opened and not closed. */
    get openSockets(): FakeCollaborationSocket[] {
        return FakeCollaborationSocket.instances.filter(socket => socket.readyState === FakeCollaborationSocket.OPEN);
    }

    dispose(): void {
        vi.unstubAllGlobals();
    }
}
