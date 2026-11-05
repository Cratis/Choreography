// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { a_collaboration_client } from '../given/a_collaboration_client.js';

describe('when the server pings a joined connection', () => {
    let context: a_collaboration_client;

    beforeEach(() => {
        context = new a_collaboration_client();
        context.socket.deliver({ type: 'ping' });
    });

    afterEach(() => context.dispose());

    it('should answer with a pong so intermediaries see two-way traffic', () =>
        expect(context.socket.sentMessages).toContainEqual({ type: 'pong' }));
});
