// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GuardedStorage } from './GuardedStorage.js';

const guarded = new Map<'local' | 'session', Storage>();

/**
 * Gets the browser's local or session storage, guarded so that it never throws. Where the browser refuses
 * access, the storage lives in memory for the page instead.
 * @param kind `local` for the storage shared by every tab, `session` for the one of this tab.
 * @returns The storage.
 */
export const browserStorage = (kind: 'local' | 'session'): Storage => {
    let storage = guarded.get(kind);
    if (!storage) {
        let inner: Storage | undefined;
        try {
            inner = kind === 'local' ? globalThis.localStorage : globalThis.sessionStorage;
        } catch {
            inner = undefined;
        }
        storage = new GuardedStorage(inner);
        guarded.set(kind, storage);
    }
    return storage;
};
