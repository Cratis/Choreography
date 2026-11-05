// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi, type Mock } from 'vitest';
import { CollaborationClient } from '../../CollaborationClient.js';
import type { CollaboratorInfo } from '../../CollaboratorInfo.js';
import { FakeCollaborationSocket } from '../../testing/FakeCollaborationSocket.js';
import { collaboratorInfoOf } from '../../testing/collaboratorInfoOf.js';

/** The remote collaborator's session, already on the roster before each spec's action runs. */
export const REMOTE_SESSION_ID = 'remote-session';

/** The remote collaborator's starting position, in surface content coordinates. */
export const STARTING_POSITION = { x: 10, y: 20 };

/** The remote collaborator's starting area and surface. */
export const STARTING_AREA = 'board';
export const STARTING_SURFACE_ID = 'surface-1';

/**
 * A {@link CollaborationClient} wired to a {@link FakeCollaborationSocket} under fake timers, joined to a
 * room with one remote collaborator already on the roster at a known starting position, and subscribed to by
 * a spy - the state every spec of the client starts from.
 */
export class a_collaboration_client {
    readonly client: CollaborationClient;
    readonly listener: Mock<() => void> = vi.fn<() => void>();
    readonly consoleError = vi.spyOn(console, 'error');
    readonly consoleWarning = vi.spyOn(console, 'warn');

    constructor(deliverRoster = true) {
        vi.useFakeTimers();
        vi.stubGlobal('window', { location: { protocol: 'https:', host: 'test.local' } });
        vi.stubGlobal('WebSocket', FakeCollaborationSocket);
        FakeCollaborationSocket.reset();
        this.client = new CollaborationClient('room', 'session', 'fff');
        if (deliverRoster) {
            this.socket.deliver({ type: 'roster', userId: 'local-user', collaborators: [collaboratorInfoOf()] });
        }
        this.client.subscribe(this.listener);
    }

    /** The most recent socket the client opened. */
    get socket(): FakeCollaborationSocket {
        return FakeCollaborationSocket.latest;
    }

    /** The collaborator with the given session, as the client currently holds it. */
    collaborator(sessionId = REMOTE_SESSION_ID): CollaboratorInfo | undefined {
        return this.client.getCollaborators().find(candidate => candidate.sessionId === sessionId);
    }

    /** Simulates the server delivering a `presence` message for the remote collaborator. */
    deliverPresence(x: number, y: number, area = STARTING_AREA, surfaceId = STARTING_SURFACE_ID): void {
        this.socket.deliver({ type: 'presence', sessionId: REMOTE_SESSION_ID, x, y, area, surfaceId });
    }

    /** Simulates the server delivering an `idle` message for the remote collaborator. */
    deliverIdle(isIdle: boolean): void {
        this.socket.deliver({ type: 'idle', sessionId: REMOTE_SESSION_ID, isIdle });
    }

    /** Tears down the client and restores the globals this context stubbed. */
    dispose(): void {
        this.client.dispose();
        this.consoleError.mockRestore();
        this.consoleWarning.mockRestore();
        vi.unstubAllGlobals();
        vi.useRealTimers();
    }
}
