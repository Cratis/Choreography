// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { FakeCollaborationSocket } from '../../testing/FakeCollaborationSocket.js';
import { a_collaboration_client } from '../given/a_collaboration_client.js';

describe('when the server goes silent for longer than twice its ping interval', () => {
    let context: a_collaboration_client;
    let stale: FakeCollaborationSocket;

    beforeEach(() => {
        context = new a_collaboration_client();
        stale = context.socket;
        vi.advanceTimersByTime(40_000);
        vi.advanceTimersByTime(1_000);
    });

    afterEach(() => context.dispose());

    it('should tear the stale connection down', () => expect(stale.close).toHaveBeenCalled());
    it('should open a new connection', () => expect(FakeCollaborationSocket.instances).toHaveLength(2));
    it('should drop the roster so no stale cursors linger', () => expect(context.client.getCollaborators()).toEqual([]));
});

describe('when the server keeps pinging', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        for (let tick = 0; tick < 6; tick++) {
            vi.advanceTimersByTime(20_000);
            context.socket.deliver({ type: 'ping' });
        }
    });

    afterEach(() => context.dispose());

    it('should keep the connection', () => expect(FakeCollaborationSocket.instances).toHaveLength(1));
});
