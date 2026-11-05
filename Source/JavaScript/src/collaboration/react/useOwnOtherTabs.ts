// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useSyncExternalStore } from 'react';
import type { CollaborationStoreOptions } from '../CollaborationStoreOptions.js';
import { useCollaborationClient } from './useCollaborationClient.js';

const noSubscription = () => () => { /* nothing to unsubscribe */ };

/**
 * Subscribes to how many other sessions (tabs, windows, devices) the signed-in user has open in a room.
 * @param room The room to observe, when known.
 * @param options How to reach the backend; applied when this hook is the first to connect.
 * @returns The number of the user's other sessions.
 */
export const useOwnOtherTabs = (room: string | undefined, options?: CollaborationStoreOptions): number => {
    const client = useCollaborationClient(room, options);
    const subscribe = useCallback(
        (listener: () => void) => (client ? client.subscribe(listener) : noSubscription()),
        [client]);
    const getSnapshot = useCallback(() => (client ? client.getOwnOtherTabs() : 0), [client]);

    return useSyncExternalStore(subscribe, getSnapshot, () => 0);
};
