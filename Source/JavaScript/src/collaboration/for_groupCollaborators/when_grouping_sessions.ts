// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { groupCollaborators } from '../groupCollaborators.js';
import { collaboratorInfoOf } from '../testing/collaboratorInfoOf.js';

const session = (sessionId: string, userId: string, overrides = {}) =>
    collaboratorInfoOf({ sessionId, userId, displayName: userId, x: 0, y: 0, area: 'board', ...overrides });

describe('when grouping sessions', () => {
    describe('and one user has an idle and an active tab', () => {
        const groups = groupCollaborators([
            session('idle-tab', 'einar', { isIdle: true, area: 'brainstorming', color: 'AAAAAA' }),
            session('active-tab', 'einar'),
            session('alice-tab', 'alice', { isIdle: true })
        ]);

        it('should list each user once', () => expect(groups.map(group => group.userId)).toEqual(['einar', 'alice']));
        it('should count the tabs of the user', () => expect(groups[0].tabs).toBe(2));
        it('should not call the user idle while one tab is active', () => expect(groups[0].isIdle).toBe(false));
        it('should show the area of the active tab', () => expect(groups[0].area).toBe('board'));
        it('should keep the color of the first session', () => expect(groups[0].color).toBe('AAAAAA'));
        it('should call a user idle when all the tabs are idle', () => expect(groups[1].isIdle).toBe(true));
    });

    describe('and only one of a user\'s tabs has an avatar', () => {
        const groups = groupCollaborators([session('a', 'einar'), session('b', 'einar', { hasAvatar: true })]);

        it('should say the user has an avatar', () => expect(groups[0].hasAvatar).toBe(true));
    });

    describe('and nobody is present', () => {
        it('should have no entries', () => expect(groupCollaborators([])).toEqual([]));
    });
});
