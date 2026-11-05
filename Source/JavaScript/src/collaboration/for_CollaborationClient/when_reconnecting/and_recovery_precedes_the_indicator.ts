// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CollaborationConnectionState } from '../../CollaborationConnectionState.js';
import { a_collaboration_client } from '../given/a_collaboration_client.js';

describe('when collaboration recovers before the indicator deadline', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        context.socket.onclose?.();
        vi.advanceTimersByTime(1_000);
        context.socket.deliver({ type: 'roster', collaborators: [] });
        vi.advanceTimersByTime(29_001);
    });

    afterEach(() => context.dispose());

    it('should cancel the delayed indicator', () =>
        expect(context.client.getConnectionState()).toBe(CollaborationConnectionState.Connected));
});
