// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import { MemoryStorage } from '../MemoryStorage.js';
import { claimSessionId, releaseSession, vouchForSession } from '../sessionIdentity.js';

describe('when claiming a session id', () => {
    let shared: MemoryStorage;
    let counter: number;
    const create = () => `created-${++counter}`;

    beforeEach(() => {
        shared = new MemoryStorage();
        counter = 0;
    });

    describe('and the page is new', () => {
        it('should create an id and remember it for reloads', () => {
            const session = new MemoryStorage();
            expect(claimSessionId(session, shared, 1_000, create)).toBe('created-1');
            expect(session.getItem('collaboration:sessionId')).toBe('created-1');
        });
    });

    describe('and the page is reloaded', () => {
        it('should reuse the id once the previous page released it', () => {
            const session = new MemoryStorage();
            const first = claimSessionId(session, shared, 1_000, create);
            releaseSession(shared, first);
            expect(claimSessionId(session, shared, 1_500, create)).toBe(first);
        });

        it('should reuse the id when the previous page stopped vouching for it', () => {
            const session = new MemoryStorage();
            const first = claimSessionId(session, shared, 1_000, create);
            expect(claimSessionId(session, shared, 1_000 + 7_000, create)).toBe(first);
        });
    });

    describe('and the tab was duplicated while the original is open', () => {
        it('should not share the id of the live page', () => {
            const original = new MemoryStorage();
            const originalId = claimSessionId(original, shared, 1_000, create);
            vouchForSession(shared, originalId, 2_000);

            const duplicate = new MemoryStorage();
            duplicate.setItem('collaboration:sessionId', originalId);
            expect(claimSessionId(duplicate, shared, 3_000, create)).not.toBe(originalId);
        });
    });

    describe('and a vouch is left over from a crashed page long ago', () => {
        it('should sweep it', () => {
            vouchForSession(shared, 'crashed', 1_000);
            claimSessionId(new MemoryStorage(), shared, 1_000 + 2 * 60 * 60 * 1000, create);
            expect(shared.getItem('collaboration:alive:crashed')).toBeNull();
        });
    });
});
