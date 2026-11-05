// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CollaborationConnectionState } from '../../CollaborationConnectionState.js';
import { FakeCollaborationSocket } from '../../testing/FakeCollaborationSocket.js';
import { a_collaboration_client } from '../given/a_collaboration_client.js';

describe('when a rollout briefly closes collaboration', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        context.socket.onclose?.();
        vi.advanceTimersByTime(29_999);
    });

    afterEach(() => context.dispose());

    it('should remain quiet before thirty seconds', () =>
        expect(context.client.getConnectionState()).toBe(CollaborationConnectionState.Connecting));
    it('should rejoin the same room and session after backoff', () => {
        expect(FakeCollaborationSocket.instances).toHaveLength(2);
        expect(context.socket.url).toBe('wss://test.local/ws/collaboration?room=room&sessionId=session&color=fff');
    });
    it('should not log rollout errors', () => expect(context.consoleError).not.toHaveBeenCalled());
    it('should not log rollout warnings', () => expect(context.consoleWarning).not.toHaveBeenCalled());
});
