// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { a_collaboration_client, STARTING_POSITION } from '../given/a_collaboration_client.js';

describe('when a presence update repeats the position of a collaborator who was idle', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        context.deliverIdle(true);
        context.deliverPresence(STARTING_POSITION.x, STARTING_POSITION.y);
    });

    afterEach(() => context.dispose());

    // Only moving the cursor is activity (the server's own rule): a repeated position - the replay after a
    // reconnect, a zoom - must not turn a really idle collaborator into an active one.
    it('should keep the collaborator idle', () => expect(context.collaborator()?.isIdle).toBe(true));
});
