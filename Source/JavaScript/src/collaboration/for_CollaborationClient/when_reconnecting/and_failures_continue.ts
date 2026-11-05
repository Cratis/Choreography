// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CollaborationConnectionState } from '../../CollaborationConnectionState.js';
import { FakeCollaborationSocket } from '../../testing/FakeCollaborationSocket.js';
import { a_collaboration_client } from '../given/a_collaboration_client.js';

describe('when collaboration repeatedly fails to rejoin', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        context.socket.onerror?.();
        vi.advanceTimersByTime(1_000);
        context.socket.onopen?.();
        context.socket.onclose?.();
        vi.advanceTimersByTime(1_500);
        context.socket.onclose?.();
        vi.advanceTimersByTime(27_500);
    });

    afterEach(() => context.dispose());

    it('should expose the delayed reconnecting indicator', () =>
        expect(context.client.getConnectionState()).toBe(CollaborationConnectionState.Reconnecting));
    it('should keep retrying without treating an upgrade as a successful join', () =>
        expect(FakeCollaborationSocket.instances).toHaveLength(4));
    it('should notify connection state subscribers', () => expect(context.listener).toHaveBeenCalled());
    it('should not log connection failures', () => expect(context.consoleError).not.toHaveBeenCalled());
});
