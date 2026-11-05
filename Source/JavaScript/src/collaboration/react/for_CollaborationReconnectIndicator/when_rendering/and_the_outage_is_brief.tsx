// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderToStaticMarkup } from 'react-dom/server';
import { describe, expect, it } from 'vitest';
import { CollaborationConnectionState } from '../../../CollaborationConnectionState.js';
import { CollaborationReconnectIndicator } from '../../CollaborationReconnectIndicator.js';

describe('when rendering collaboration before the reconnect threshold', () => {
    for (const state of [CollaborationConnectionState.Connecting, CollaborationConnectionState.Connected]) {
        it(`should show nothing for state ${state}`, () =>
            expect(renderToStaticMarkup(<CollaborationReconnectIndicator state={state} label='Reconnecting collaboration…' />)).toBe(''));
    }
});
