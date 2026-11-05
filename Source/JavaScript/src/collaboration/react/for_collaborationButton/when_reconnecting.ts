// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { CollaborationConnectionState } from '../../CollaborationConnectionState.js';
import { collaborationButton } from '../collaborationButton.js';

describe('when reconnecting', () => {
    const button = collaborationButton(CollaborationConnectionState.Reconnecting, 'Collaborators', 'Reconnecting collaboration…');

    it('should mark the button as reconnecting', () => expect(button.className).toContain('collaboration-indicator--reconnecting'));
    it('should explain the reconnect on hover', () => expect(button.title).toBe('Reconnecting collaboration…'));
});
