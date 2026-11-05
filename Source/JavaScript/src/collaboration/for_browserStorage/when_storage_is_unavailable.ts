// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, describe, expect, it, vi } from 'vitest';
import { GuardedStorage } from '../GuardedStorage.js';

/** A storage that throws on every use, as browsers do in private windows or when the quota is full. */
const throwingStorage = (): Storage => {
    const fail = () => {
        throw new DOMException('The quota has been exceeded.', 'QuotaExceededError');
    };
    return { get length() { return fail(); }, clear: fail, getItem: fail, key: fail, removeItem: fail, setItem: fail } as unknown as Storage;
};

describe('when storage is unavailable', () => {
    afterEach(() => vi.restoreAllMocks());

    describe('and there is none at all', () => {
        it('should keep items in memory', () => {
            const storage = new GuardedStorage();
            storage.setItem('key', 'value');
            expect(storage.getItem('key')).toBe('value');
            expect(storage.length).toBe(1);
            expect(storage.key(0)).toBe('key');
        });

        it('should forget an item that is removed', () => {
            const storage = new GuardedStorage();
            storage.setItem('key', 'value');
            storage.removeItem('key');
            expect(storage.getItem('key')).toBeNull();
        });
    });

    describe('and every use of it throws', () => {
        it('should not throw and should keep items in memory', () => {
            const storage = new GuardedStorage(throwingStorage());
            expect(() => storage.setItem('key', 'value')).not.toThrow();
            expect(storage.getItem('key')).toBe('value');
            expect(storage.length).toBe(1);
            expect(storage.key(0)).toBe('key');
            expect(() => storage.removeItem('key')).not.toThrow();
            expect(storage.getItem('key')).toBeNull();
            expect(() => storage.clear()).not.toThrow();
        });
    });

    describe('and only writing throws', () => {
        it('should still return what it was given', () => {
            const inner = new Map<string, string>();
            const storage = new GuardedStorage({
                get length() { return inner.size; },
                clear: () => inner.clear(),
                getItem: (key: string) => inner.get(key) ?? null,
                key: (index: number) => [...inner.keys()][index] ?? null,
                removeItem: (key: string) => { inner.delete(key); },
                setItem: () => { throw new DOMException('The quota has been exceeded.', 'QuotaExceededError'); }
            });

            storage.setItem('key', 'value');

            expect(storage.getItem('key')).toBe('value');
        });
    });

    describe('and storage works', () => {
        it('should pass items through to it', () => {
            const inner = new Map<string, string>();
            const storage = new GuardedStorage({
                get length() { return inner.size; },
                clear: () => inner.clear(),
                getItem: (key: string) => inner.get(key) ?? null,
                key: (index: number) => [...inner.keys()][index] ?? null,
                removeItem: (key: string) => { inner.delete(key); },
                setItem: (key: string, value: string) => { inner.set(key, value); }
            });

            storage.setItem('key', 'value');

            expect(inner.get('key')).toBe('value');
        });
    });
});
