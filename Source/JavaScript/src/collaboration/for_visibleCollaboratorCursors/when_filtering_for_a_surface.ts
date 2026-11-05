// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import { visibleCollaboratorCursors } from '../visibleCollaboratorCursors.js';
import type { CollaboratorInfo } from '../CollaboratorInfo.js';
import { collaboratorInfoOf } from '../testing/collaboratorInfoOf.js';

const collaborator = (area: string, surfaceId: string) => collaboratorInfoOf({ area, surfaceId });

describe('when collaborators are spread across surfaces and areas', () => {
    let result: CollaboratorInfo[];

    beforeEach(() => {
        result = visibleCollaboratorCursors(
            [collaborator('board', 'surface-1'), collaborator('board', 'surface-2'), collaborator('notes', 'surface-1')],
            'board',
            'surface-1');
    });

    it('should keep only collaborators on the same surface in the same area', () => expect(result).toHaveLength(1));
    it('should keep the matching collaborator', () => expect(result[0].surfaceId).toBe('surface-1'));
});

describe('when a collaborator is viewing a different surface', () => {
    it('should exclude their cursor', () =>
        expect(visibleCollaboratorCursors([collaborator('board', 'surface-2')], 'board', 'surface-1')).toHaveLength(0));
});

describe('when filtering for another area', () => {
    let result: CollaboratorInfo[];

    beforeEach(() => {
        result = visibleCollaboratorCursors([collaborator('notes', 'surface-1'), collaborator('board', 'surface-1')], 'notes', 'surface-1');
    });

    it('should keep only the collaborator in that area', () => expect(result).toHaveLength(1));
    it('should keep the collaborator in the area', () => expect(result[0].area).toBe('notes'));
});

describe('when a collaborator is in another area of the same surface', () => {
    it('should exclude their cursor', () =>
        expect(visibleCollaboratorCursors([collaborator('notes', 'surface-1')], 'board', 'surface-1')).toHaveLength(0));
});

describe('when a collaborator has left every area', () => {
    it('should exclude their cursor', () =>
        expect(visibleCollaboratorCursors([collaborator('', '')], 'board', 'surface-1')).toHaveLength(0));
});
