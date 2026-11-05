// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CollaboratorGroup } from './CollaboratorGroup.js';
import type { CollaboratorInfo } from './CollaboratorInfo.js';

/**
 * Groups the remote sessions into one entry per user, so a person with two tabs open is listed once with a
 * tab count rather than twice. The entry reflects the user's most active session: idle only when all are.
 * @param collaborators The remote sessions.
 * @returns One entry per user, in the order the users first appear.
 */
export const groupCollaborators = (collaborators: CollaboratorInfo[]): CollaboratorGroup[] => {
    const groups = new Map<string, CollaboratorGroup>();
    for (const collaborator of collaborators) {
        const existing = groups.get(collaborator.userId);
        if (!existing) {
            groups.set(collaborator.userId, {
                userId: collaborator.userId,
                displayName: collaborator.displayName,
                color: collaborator.color,
                hasAvatar: collaborator.hasAvatar === true,
                area: collaborator.area,
                isIdle: collaborator.isIdle,
                tabs: 1
            });
            continue;
        }

        existing.tabs++;
        existing.hasAvatar ||= collaborator.hasAvatar === true;
        if (existing.isIdle && !collaborator.isIdle) {
            existing.isIdle = false;
            existing.area = collaborator.area;
        }
    }

    return [...groups.values()];
};
