// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { a_collaboration_client } from '../given/a_collaboration_client.js';

describe('when the session leaves the surface it was on', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        context.client.sendPresence(10, 20, 'board', 'surface-1');
        context.client.leaveArea();

        // Presence sends are throttled: the latest one goes out when the interval has passed.
        vi.advanceTimersByTime(80);
    });

    afterEach(() => context.dispose());

    it('should tell the others it is in no area any more, so its cursor is not drawn where it was', () => {
        const presences = context.socket.sentMessages.filter(message => message.type === 'presence');
        const last = presences[presences.length - 1];
        expect(last.area).toBe('');
        expect(last.surfaceId).toBe('');
    });
});
