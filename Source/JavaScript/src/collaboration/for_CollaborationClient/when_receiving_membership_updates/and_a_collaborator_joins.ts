// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { a_collaboration_client } from '../given/a_collaboration_client.js';
import { collaboratorInfoOf } from '../../testing/collaboratorInfoOf.js';

describe('when a collaborator joins the room', () => {
    let context: a_collaboration_client;
    let notifications: number;

    beforeEach(() => {
        context = new a_collaboration_client();
        notifications = context.listener.mock.calls.length;
        context.socket.deliver({ type: 'joined', collaborator: collaboratorInfoOf({ sessionId: 'newcomer', userId: 'user-2' }) });
    });

    afterEach(() => context.dispose());

    it('should hold the newcomer', () => expect(context.collaborator('newcomer')).toBeDefined());
    it('should keep the existing collaborator', () => expect(context.collaborator()).toBeDefined());
    it('should notify subscribers once', () => expect(context.listener.mock.calls.length).toBe(notifications + 1));
});

describe('when a join arrives without a collaborator', () => {
    let context: a_collaboration_client;
    let notifications: number;

    beforeEach(() => {
        context = new a_collaboration_client();
        notifications = context.listener.mock.calls.length;
        context.socket.deliver({ type: 'joined' });
    });

    afterEach(() => context.dispose());

    it('should not notify subscribers', () => expect(context.listener.mock.calls.length).toBe(notifications));
});
