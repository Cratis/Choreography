// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * A {@link Storage} that keeps its items in memory. It stands in for the browser's storage where that is
 * not available - a private window, blocked site data - so nothing that reads or writes storage has to care.
 */
export class MemoryStorage implements Storage {
    readonly #items = new Map<string, string>();

    /** @inheritdoc */
    get length(): number {
        return this.#items.size;
    }

    /** @inheritdoc */
    clear(): void {
        this.#items.clear();
    }

    /** @inheritdoc */
    getItem(key: string): string | null {
        return this.#items.get(key) ?? null;
    }

    /** @inheritdoc */
    key(index: number): string | null {
        return [...this.#items.keys()][index] ?? null;
    }

    /** @inheritdoc */
    removeItem(key: string): void {
        this.#items.delete(key);
    }

    /** @inheritdoc */
    setItem(key: string, value: string): void {
        this.#items.set(key, value);
    }
}
