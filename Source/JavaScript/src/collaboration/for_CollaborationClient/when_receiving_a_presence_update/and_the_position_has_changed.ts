// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { a_collaboration_client } from '../given/a_collaboration_client.js';

describe('when a presence update carries a new position', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        context.listener.mockClear();
        context.deliverPresence(30, 40);
    });

    afterEach(() => context.dispose());

    it('should publish a new snapshot', () => expect(context.listener).toHaveBeenCalledTimes(1));
    it('should update the collaborator to the new horizontal position', () => expect(context.collaborator()?.x).toBe(30));
    it('should update the collaborator to the new vertical position', () => expect(context.collaborator()?.y).toBe(40));
});
