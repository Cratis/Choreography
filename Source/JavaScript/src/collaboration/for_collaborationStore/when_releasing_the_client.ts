// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { FakeCollaborationSocket } from '../testing/FakeCollaborationSocket.js';
import { acquireCollaboration, currentCollaboration, releaseCollaboration } from '../collaborationStore.js';
import { a_collaboration_store } from './given/a_collaboration_store.js';

describe('when releasing the client', () => {
    let context: a_collaboration_store;

    beforeEach(() => {
        context = new a_collaboration_store();
    });

    afterEach(() => context.dispose());

    describe('and another holder still has it', () => {
        it('should keep the connection open', () => {
            const client = acquireCollaboration('room-1');
            acquireCollaboration('room-1');
            releaseCollaboration(client);
            expect(context.openSockets).toHaveLength(1);
            expect(currentCollaboration()).toBe(client);
            releaseCollaboration(client);
        });
    });

    describe('and the last holder releases it', () => {
        it('should close the connection, leaving the room', () => {
            const client = acquireCollaboration('room-1');
            acquireCollaboration('room-1');
            releaseCollaboration(client);
            releaseCollaboration(client);
            expect(FakeCollaborationSocket.latest.close).toHaveBeenCalled();
        });

        it('should have no current client any more', () => {
            const client = acquireCollaboration('room-1');
            releaseCollaboration(client);
            expect(currentCollaboration()).toBeUndefined();
        });

        it('should open a new connection when acquired again', () => {
            releaseCollaboration(acquireCollaboration('room-1'));
            acquireCollaboration('room-1');
            expect(FakeCollaborationSocket.instances).toHaveLength(2);
            releaseCollaboration(currentCollaboration()!);
        });
    });

    describe('and the client is no longer the active one', () => {
        it('should leave the active client alone', () => {
            const stale = acquireCollaboration('room-1');
            const active = acquireCollaboration('room-2');
            releaseCollaboration(stale);
            expect(currentCollaboration()).toBe(active);
            expect(context.openSockets).toHaveLength(1);
            releaseCollaboration(active);
        });
    });

    describe('and the page is hidden for good', () => {
        it('should close the connection', () => {
            acquireCollaboration('room-1');
            window.dispatchEvent(Object.assign(new Event('pagehide'), { persisted: false }));
            expect(currentCollaboration()).toBeUndefined();
            expect(context.openSockets).toHaveLength(0);
        });
    });

    describe('and the page is only being cached', () => {
        it('should keep the connection', () => {
            const client = acquireCollaboration('room-1');
            window.dispatchEvent(Object.assign(new Event('pagehide'), { persisted: true }));
            expect(currentCollaboration()).toBe(client);
            releaseCollaboration(client);
        });
    });
});
