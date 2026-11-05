// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { a_collaboration_client, STARTING_POSITION } from '../given/a_collaboration_client.js';

describe('when an idle collaborator moves the cursor', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        context.deliverIdle(true);
        context.deliverPresence(STARTING_POSITION.x + 5, STARTING_POSITION.y);
    });

    afterEach(() => context.dispose());

    it('should clear the idle flag', () => expect(context.collaborator()?.isIdle).toBe(false));
});
