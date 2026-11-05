// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CollaboratorAvatarSubject } from './CollaboratorAvatarSubject.js';

/**
 * One person in the collaborators list: every session (tab) of one user is a single entry.
 */
export interface CollaboratorGroup extends CollaboratorAvatarSubject {
    /** Whether the user has an uploaded avatar. */
    hasAvatar: boolean;

    /** The area of the session the user was last seen active in (an idle session only when every session is idle). */
    area: string;

    /** Whether every session of the user is idle. */
    isIdle: boolean;

    /** How many sessions (tabs) the user has open. */
    tabs: number;
}
