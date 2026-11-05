// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CollaboratorInfo } from './CollaboratorInfo.js';

/**
 * Selects the collaborators whose cursors should be drawn on a given surface - those currently in the same
 * area AND looking at the same surface. A collaborator on a different surface (or in another area) must not
 * have their cursor rendered here.
 * @param collaborators The other collaborators in the room.
 * @param area The area the local user is in.
 * @param surfaceId The identifier of the surface being viewed.
 * @returns The collaborators to render cursors for.
 */
export const visibleCollaboratorCursors = (collaborators: CollaboratorInfo[], area: string, surfaceId: string): CollaboratorInfo[] =>
    collaborators.filter(collaborator =>
        collaborator.area === area &&
        collaborator.surfaceId?.toString() === surfaceId);
