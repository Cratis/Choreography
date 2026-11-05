// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { CollaboratorInfo } from '../../CollaboratorInfo.js';
import { a_collaboration_client, STARTING_POSITION } from '../given/a_collaboration_client.js';

describe('when a presence update repeats the same position, area and surface', () => {
    let context: a_collaboration_client;
    let snapshotBeforeUpdate: CollaboratorInfo[];

    beforeEach(() => {
        context = new a_collaboration_client();
        snapshotBeforeUpdate = context.client.getCollaborators();
        context.listener.mockClear();
        context.deliverPresence(STARTING_POSITION.x, STARTING_POSITION.y);
    });

    afterEach(() => context.dispose());

    // useSyncExternalStore compares snapshots by reference - a redundant publish that rebuilds the array even
    // though nothing changed forces every subscriber to re-render for no reason, and under a fast enough stream
    // of these no-op updates that churn feeds a "maximum update depth exceeded" crash.
    it('should not publish a new snapshot', () => expect(context.client.getCollaborators()).toBe(snapshotBeforeUpdate));
    it('should not notify subscribers', () => expect(context.listener).not.toHaveBeenCalled());
});
