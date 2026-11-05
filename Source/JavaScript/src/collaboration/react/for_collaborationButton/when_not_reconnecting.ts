// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { CollaborationConnectionState } from '../../CollaborationConnectionState.js';
import { collaborationButton } from '../collaborationButton.js';

describe('when not reconnecting', () => {
    for (const state of [CollaborationConnectionState.Connecting, CollaborationConnectionState.Connected]) {
        const button = collaborationButton(state, 'Collaborators', 'Reconnecting collaboration…');
        it(`should keep the ordinary button for state ${state}`, () => expect(button.className).toBe('collaboration-indicator'));
        it(`should keep the ordinary tooltip for state ${state}`, () => expect(button.title).toBe('Collaborators'));
    }
});
