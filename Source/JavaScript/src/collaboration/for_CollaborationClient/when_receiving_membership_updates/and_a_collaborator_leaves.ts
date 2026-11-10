// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { a_collaboration_client, REMOTE_SESSION_ID } from '../given/a_collaboration_client.js';

describe('when a known collaborator leaves the room', () => {
    let context: a_collaboration_client;
    let notifications: number;

    beforeEach(() => {
        context = new a_collaboration_client();
        notifications = context.listener.mock.calls.length;
        context.socket.deliver({ type: 'left', sessionId: REMOTE_SESSION_ID });
    });

    afterEach(() => context.dispose());

    it('should drop the collaborator', () => expect(context.collaborator()).toBeUndefined());
    it('should notify subscribers once', () => expect(context.listener.mock.calls.length).toBe(notifications + 1));
});

describe('when an unknown session leaves the room', () => {
    let context: a_collaboration_client;
    let notifications: number;

    beforeEach(() => {
        context = new a_collaboration_client();
        notifications = context.listener.mock.calls.length;
        context.socket.deliver({ type: 'left', sessionId: 'stranger' });
    });

    afterEach(() => context.dispose());

    it('should keep the collaborator', () => expect(context.collaborator()).toBeDefined());
    it('should not notify subscribers', () => expect(context.listener.mock.calls.length).toBe(notifications));
});
