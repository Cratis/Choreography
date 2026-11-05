// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { FakeCollaborationSocket } from '../../testing/FakeCollaborationSocket.js';
import { a_collaboration_client } from '../given/a_collaboration_client.js';

describe('when collaboration is disposed during reconnect backoff', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        context.socket.onerror?.();
        context.client.dispose();
        context.listener.mockClear();
        vi.advanceTimersByTime(120_000);
    });

    afterEach(() => context.dispose());

    it('should not open another socket', () => expect(FakeCollaborationSocket.instances).toHaveLength(1));
    it('should not notify a delayed indicator', () => expect(context.listener).not.toHaveBeenCalled());
});
