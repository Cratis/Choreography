// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Creates an identifier for a collaboration session.
 * @returns A new, unique identifier.
 */
export const createSessionId = (): string => {
    if (typeof globalThis.crypto?.randomUUID === 'function') {
        return globalThis.crypto.randomUUID();
    }

    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, character => {
        const value = Math.floor(Math.random() * 16);
        return (character === 'x' ? value : (value & 0x3) | 0x8).toString(16);
    });
};
