// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, expect, it, vi } from 'vitest';
import { createSessionId } from '../createSessionId.js';

describe('when creating a session id', () => {
    afterEach(() => vi.unstubAllGlobals());

    it('should be unique each time', () => expect(createSessionId()).not.toBe(createSessionId()));

    it('should look like a uuid even without the crypto api', () => {
        vi.stubGlobal('crypto', undefined);
        expect(createSessionId()).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/);
    });
});
