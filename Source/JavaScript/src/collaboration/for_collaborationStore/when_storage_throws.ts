// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { FakeCollaborationSocket } from '../testing/FakeCollaborationSocket.js';

const denied = () => {
    throw new DOMException('The operation is insecure.', 'SecurityError');
};

describe('when the browser refuses access to storage', () => {
    let originalLocal: PropertyDescriptor | undefined;
    let originalSession: PropertyDescriptor | undefined;

    beforeEach(() => {
        originalLocal = Object.getOwnPropertyDescriptor(globalThis, 'localStorage');
        originalSession = Object.getOwnPropertyDescriptor(globalThis, 'sessionStorage');
        Object.defineProperty(globalThis, 'localStorage', { configurable: true, get: denied });
        Object.defineProperty(globalThis, 'sessionStorage', { configurable: true, get: denied });
        vi.stubGlobal('WebSocket', FakeCollaborationSocket);
        FakeCollaborationSocket.reset();
        vi.resetModules();
    });

    afterEach(() => {
        for (const [name, descriptor] of [['localStorage', originalLocal], ['sessionStorage', originalSession]] as const) {
            if (descriptor) Object.defineProperty(globalThis, name, descriptor);
            else delete (globalThis as Record<string, unknown>)[name];
        }
        vi.unstubAllGlobals();
    });

    it('should still connect, with a session id and a color that last as long as the page', async () => {
        const { acquireCollaboration, releaseCollaboration } = await import('../collaborationStore.js');

        const first = acquireCollaboration('room-1');
        const second = acquireCollaboration('room-2');

        expect(first.sessionId).not.toBe('');
        expect(first.color).toMatch(/^[0-9A-F]{6}$/);
        expect(second.sessionId).toBe(first.sessionId);
        expect(second.color).toBe(first.color);
        releaseCollaboration(second);
    });

    it('should hand out the browser storage guarded, so nothing throws', async () => {
        const { browserStorage } = await import('../browserStorage.js');

        const storage = browserStorage('local');
        storage.setItem('key', 'value');

        expect(storage.getItem('key')).toBe('value');
    });
});
