// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useSyncExternalStore } from 'react';
import { CollaborationConnectionState } from '../CollaborationConnectionState.js';
import type { CollaborationStoreOptions } from '../CollaborationStoreOptions.js';
import { useCollaborationClient } from './useCollaborationClient.js';

const noSubscription = () => () => { /* nothing to unsubscribe */ };

/**
 * Subscribes to the state of the connection to the collaboration backend.
 * @param room The room, when known.
 * @param options How to reach the backend; applied when this hook is the first to connect.
 * @returns The connection state; `Connecting` while there is no client yet.
 */
export const useCollaborationConnectionState = (room: string | undefined, options?: CollaborationStoreOptions): CollaborationConnectionState => {
    const client = useCollaborationClient(room, options);
    const subscribe = useCallback(
        (listener: () => void) => (client ? client.subscribe(listener) : noSubscription()),
        [client]);
    const getSnapshot = useCallback(
        () => (client ? client.getConnectionState() : CollaborationConnectionState.Connecting),
        [client]);

    return useSyncExternalStore(subscribe, getSnapshot, () => CollaborationConnectionState.Connecting);
};
