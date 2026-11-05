// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { FakeCollaborationSocket } from '../../testing/FakeCollaborationSocket.js';
import { a_collaboration_client } from '../given/a_collaboration_client.js';

describe('when the transport reports both error and close', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        const lateClose = context.socket.onclose;
        context.socket.onerror?.();
        lateClose?.();
        vi.advanceTimersByTime(1_000);
    });

    afterEach(() => context.dispose());

    it('should schedule only one reconnect', () => expect(FakeCollaborationSocket.instances).toHaveLength(2));
});
