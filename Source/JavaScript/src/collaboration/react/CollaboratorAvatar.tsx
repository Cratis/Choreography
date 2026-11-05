// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CollaboratorAvatarSubject } from '../CollaboratorAvatarSubject.js';
import { initialsOf } from '../initialsOf.js';

/**
 * Properties for {@link CollaboratorAvatar}.
 */
export interface CollaboratorAvatarProperties {
    /** The person to draw. */
    subject: CollaboratorAvatarSubject;

    /** The size in pixels, both wide and high. */
    size: number;
}

/**
 * The default avatar: the person's initials on a circle of their color.
 * @param properties The person and the size.
 * @returns The avatar.
 */
export const CollaboratorAvatar = ({ subject, size }: CollaboratorAvatarProperties) => (
    <span
        className='collaboration-avatar'
        style={{ width: size, height: size, fontSize: Math.max(Math.round(size * 0.42), 6), background: `#${subject.color}` }}>
        {initialsOf(subject.displayName)}
    </span>
);
