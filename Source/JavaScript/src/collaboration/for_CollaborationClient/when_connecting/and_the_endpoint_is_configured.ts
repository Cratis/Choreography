// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, expect, it, vi } from 'vitest';
import { CollaborationClient } from '../../CollaborationClient.js';
import { FakeCollaborationSocket } from '../../testing/FakeCollaborationSocket.js';

describe('when connecting to a configured endpoint', () => {
    const clients: CollaborationClient[] = [];

    const connect = (options: ConstructorParameters<typeof CollaborationClient>[3]) => {
        vi.stubGlobal('window', { location: { protocol: 'http:', host: 'app.local:8080' } });
        vi.stubGlobal('WebSocket', FakeCollaborationSocket);
        FakeCollaborationSocket.reset();
        const client = new CollaborationClient('room a', 'session-1', 'ABCDEF', options);
        clients.push(client);
        return FakeCollaborationSocket.latest;
    };

    afterEach(() => {
        clients.splice(0).forEach(client => client.dispose());
        vi.unstubAllGlobals();
    });

    it('should use the default path on the page host, encoding the room', () =>
        expect(connect({}).url).toBe('ws://app.local:8080/ws/collaboration?room=room%20a&sessionId=session-1&color=ABCDEF'));

    it('should use a path from the options', () =>
        expect(connect({ path: '/live/presence' }).url).toBe('ws://app.local:8080/live/presence?room=room%20a&sessionId=session-1&color=ABCDEF'));

    it('should use the url builder from the options instead of a path', () => {
        const url = connect({
            path: '/ignored',
            buildUrl: connection => `wss://elsewhere.test/hub/${connection.room}/${connection.sessionId}/${connection.color}`
        }).url;
        expect(url).toBe('wss://elsewhere.test/hub/room a/session-1/ABCDEF');
    });
});
