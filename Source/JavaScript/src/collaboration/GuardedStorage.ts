// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { MemoryStorage } from './MemoryStorage.js';

/**
 * A {@link Storage} that never throws. Browsers throw on access to storage in private windows, with blocked
 * site data, or when the quota is full; a guarded storage falls back to keeping the item in memory for this
 * page, so the page behaves the same without remembering anything beyond it.
 */
export class GuardedStorage implements Storage {
    readonly #memory = new MemoryStorage();

    /**
     * Initializes a new instance of the GuardedStorage class.
     * @param inner The browser storage to guard, or undefined when there is none to be had.
     */
    constructor(readonly inner?: Storage) {
    }

    /** @inheritdoc */
    get length(): number {
        try {
            return this.inner?.length ?? this.#memory.length;
        } catch {
            return this.#memory.length;
        }
    }

    /** @inheritdoc */
    clear(): void {
        this.#memory.clear();
        try {
            this.inner?.clear();
        } catch {
            // Nothing to clear in storage that cannot be reached.
        }
    }

    /** @inheritdoc */
    getItem(key: string): string | null {
        try {
            return this.inner ? this.inner.getItem(key) ?? this.#memory.getItem(key) : this.#memory.getItem(key);
        } catch {
            return this.#memory.getItem(key);
        }
    }

    /** @inheritdoc */
    key(index: number): string | null {
        try {
            return this.inner ? this.inner.key(index) : this.#memory.key(index);
        } catch {
            return this.#memory.key(index);
        }
    }

    /** @inheritdoc */
    removeItem(key: string): void {
        this.#memory.removeItem(key);
        try {
            this.inner?.removeItem(key);
        } catch {
            // Nothing to remove from storage that cannot be reached.
        }
    }

    /** @inheritdoc */
    setItem(key: string, value: string): void {
        try {
            if (!this.inner) throw new Error('No storage');
            this.inner.setItem(key, value);
            this.#memory.removeItem(key);
        } catch {
            this.#memory.setItem(key, value);
        }
    }
}
