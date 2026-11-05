// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ReactNode } from 'react';
import type { CollaboratorAvatarSubject } from '../CollaboratorAvatarSubject.js';

/**
 * Draws the avatar of a collaborator, for a host that has real avatars to show.
 * @param subject The person to draw - a single session on a cursor, a grouped person in the indicator.
 * @param size The size in pixels the avatar is given room for, both wide and high.
 * @returns The avatar. It should fill the size it was given.
 */
export type RenderCollaboratorAvatar = (subject: CollaboratorAvatarSubject, size: number) => ReactNode;
