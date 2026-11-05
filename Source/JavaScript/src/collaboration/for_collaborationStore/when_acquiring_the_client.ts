// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { FakeCollaborationSocket } from '../testing/FakeCollaborationSocket.js';
import { acquireCollaboration, currentCollaboration, releaseCollaboration } from '../collaborationStore.js';
import { a_collaboration_store } from './given/a_collaboration_store.js';

describe('when acquiring the client', () => {
    let context: a_collaboration_store;

    beforeEach(() => {
        context = new a_collaboration_store();
    });

    afterEach(() => {
        const client = currentCollaboration();
        while (client && currentCollaboration() === client) releaseCollaboration(client);
        context.dispose();
    });

    describe('and nothing has acquired it yet', () => {
        it('should have no current client', () => expect(currentCollaboration()).toBeUndefined());
    });

    describe('and a room is acquired twice', () => {
        it('should hand out the same client', () => {
            const first = acquireCollaboration('room-1');
            const second = acquireCollaboration('room-1');
            expect(second).toBe(first);
        });

        it('should open only one connection', () => {
            acquireCollaboration('room-1');
            acquireCollaboration('room-1');
            expect(FakeCollaborationSocket.instances).toHaveLength(1);
        });

        it('should make the client the current one', () => {
            const client = acquireCollaboration('room-1');
            acquireCollaboration('room-1');
            expect(currentCollaboration()).toBe(client);
        });
    });

    describe('and another room is acquired', () => {
        it('should hand out a new client for that room', () => {
            const first = acquireCollaboration('room-1');
            const second = acquireCollaboration('room-2');
            expect(second).not.toBe(first);
            expect(second.room).toBe('room-2');
        });

        it('should close the connection of the previous room', () => {
            acquireCollaboration('room-1');
            const firstSocket = FakeCollaborationSocket.latest;
            acquireCollaboration('room-2');
            expect(firstSocket.close).toHaveBeenCalled();
            expect(context.openSockets).toHaveLength(1);
        });

        it('should keep the session id across rooms so a switch never adds a second presence', () => {
            const first = acquireCollaboration('room-1');
            const second = acquireCollaboration('room-2');
            expect(second.sessionId).toBe(first.sessionId);
        });
    });

    describe('and options are given', () => {
        it('should connect on the configured path', () => {
            acquireCollaboration('room-1', { path: '/live/presence' });
            expect(new URL(FakeCollaborationSocket.latest.url).pathname).toBe('/live/presence');
        });

        it('should use the configured color without the leading hash', () => {
            acquireCollaboration('room-1', { color: '#123ABC' });
            expect(new URL(FakeCollaborationSocket.latest.url).searchParams.get('color')).toBe('123ABC');
        });

        it('should ask the room by its name', () => {
            acquireCollaboration('room-1');
            expect(new URL(FakeCollaborationSocket.latest.url).searchParams.get('room')).toBe('room-1');
        });
    });

    describe('and no color is configured', () => {
        it('should use the same remembered color every time', () => {
            const first = acquireCollaboration('room-1');
            const second = acquireCollaboration('room-2');
            expect(second.color).toBe(first.color);
            expect(first.color).toMatch(/^[0-9A-F]{6}$/);
        });
    });
});
