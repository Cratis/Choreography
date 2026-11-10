// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { a_collaboration_client } from '../given/a_collaboration_client.js';

describe('when a collaborator goes idle', () => {
    let context: a_collaboration_client;
    let notifications: number;

    beforeEach(() => {
        context = new a_collaboration_client();
        notifications = context.listener.mock.calls.length;
        context.deliverIdle(true);
    });

    afterEach(() => context.dispose());

    it('should mark the collaborator idle', () => expect(context.collaborator()?.isIdle).toBe(true));
    it('should notify subscribers once', () => expect(context.listener.mock.calls.length).toBe(notifications + 1));
});

describe('when an idle message repeats the current state', () => {
    let context: a_collaboration_client;
    let notifications: number;

    beforeEach(() => {
        context = new a_collaboration_client();
        notifications = context.listener.mock.calls.length;
        context.deliverIdle(false);
    });

    afterEach(() => context.dispose());

    it('should not notify subscribers', () => expect(context.listener.mock.calls.length).toBe(notifications));
});

describe('when an idle message names an unknown session', () => {
    let context: a_collaboration_client;
    let notifications: number;

    beforeEach(() => {
        context = new a_collaboration_client();
        notifications = context.listener.mock.calls.length;
        context.socket.deliver({ type: 'idle', sessionId: 'stranger', isIdle: true });
    });

    afterEach(() => context.dispose());

    it('should not notify subscribers', () => expect(context.listener.mock.calls.length).toBe(notifications));
});
