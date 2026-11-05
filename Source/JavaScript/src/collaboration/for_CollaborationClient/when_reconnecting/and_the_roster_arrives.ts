// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CollaborationConnectionState } from '../../CollaborationConnectionState.js';
import { collaboratorInfoOf } from '../../testing/collaboratorInfoOf.js';
import { a_collaboration_client } from '../given/a_collaboration_client.js';

const collaborator = collaboratorInfoOf({ sessionId: 'remote', userId: 'user', displayName: 'Alice', color: 'fff', x: 1, y: 2 });

describe('when collaboration receives the new roster after a rollout', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        vi.advanceTimersByTime(100);
        context.client.sendPresence(10, 20, 'board', 'surface-1');
        context.socket.onclose?.();
        vi.advanceTimersByTime(30_000);
        context.socket.onopen?.();
        context.socket.deliver({ type: 'roster', collaborators: [collaborator] });
    });

    afterEach(() => context.dispose());

    it('should clear the delayed indicator', () =>
        expect(context.client.getConnectionState()).toBe(CollaborationConnectionState.Connected));
    it('should replace the roster with authoritative state', () =>
        expect(context.client.getCollaborators()).toEqual([collaborator]));
    it('should restore the latest local presence without another pointer move', () =>
        expect(context.socket.send).toHaveBeenCalledWith(JSON.stringify({ type: 'presence', x: 10, y: 20, area: 'board', surfaceId: 'surface-1' })));
    it('should reset the backoff after the roster confirms the join', () => {
        context.socket.onclose?.();
        vi.advanceTimersByTime(1_000);
        expect(context.socket.close).not.toHaveBeenCalled();
    });
});
