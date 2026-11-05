// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { collaboratorInfoOf } from '../../testing/collaboratorInfoOf.js';
import { a_collaboration_client, REMOTE_SESSION_ID } from '../given/a_collaboration_client.js';

const session = (sessionId: string, userId: string) => collaboratorInfoOf({ sessionId, userId, displayName: userId, area: 'board' });

describe('when the roster lists other sessions of the signed in user', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        context.socket.deliver({
            type: 'roster',
            userId: 'me',
            collaborators: [session('my-other-tab', 'me'), session('my-phone', 'me'), session(REMOTE_SESSION_ID, 'alice')]
        });
    });

    afterEach(() => context.dispose());

    it('should not treat the user as a collaborator of themselves', () =>
        expect(context.client.getCollaborators().map(collaborator => collaborator.sessionId)).toEqual([REMOTE_SESSION_ID]));
    it('should count the other sessions of the user', () => expect(context.client.getOwnOtherTabs()).toBe(2));

    describe('and one of the user\'s other tabs leaves', () => {
        beforeEach(() => context.socket.deliver({ type: 'left', sessionId: 'my-phone' }));

        it('should count one other session', () => expect(context.client.getOwnOtherTabs()).toBe(1));
    });

    describe('and another tab of the user joins', () => {
        beforeEach(() => context.socket.deliver({ type: 'joined', collaborator: session('my-third-tab', 'me') }));

        it('should still not list the user among the collaborators', () => expect(context.client.getCollaborators()).toHaveLength(1));
        it('should count the new session', () => expect(context.client.getOwnOtherTabs()).toBe(3));
    });
});
