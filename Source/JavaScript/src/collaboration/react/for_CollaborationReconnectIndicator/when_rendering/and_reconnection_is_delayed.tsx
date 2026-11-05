// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderToStaticMarkup } from 'react-dom/server';
import { describe, expect, it } from 'vitest';
import { CollaborationConnectionState } from '../../../CollaborationConnectionState.js';
import { CollaborationReconnectIndicator } from '../../CollaborationReconnectIndicator.js';

describe('when rendering collaboration after the reconnect threshold', () => {
    const html = renderToStaticMarkup(
        <CollaborationReconnectIndicator state={CollaborationConnectionState.Reconnecting} label='Kobler til samarbeid igjen…' />);

    it('should display the supplied localized label', () => expect(html).toContain('Kobler til samarbeid igjen…'));
    it('should announce the calm status accessibly', () => expect(html).toContain('role="status"'));
});
