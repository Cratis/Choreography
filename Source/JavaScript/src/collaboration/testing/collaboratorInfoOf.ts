// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CollaboratorInfo } from '../CollaboratorInfo.js';

/**
 * Builds a collaborator for specs, overriding only what the spec cares about.
 * @param overrides The values to set.
 * @returns A collaborator.
 */
export const collaboratorInfoOf = (overrides: Partial<CollaboratorInfo> = {}): CollaboratorInfo => ({
    sessionId: 'remote-session',
    userId: 'user-1',
    displayName: 'Remote Collaborator',
    color: '00FF00',
    x: 10,
    y: 20,
    area: 'board',
    surfaceId: 'surface-1',
    isIdle: false,
    ...overrides
});
