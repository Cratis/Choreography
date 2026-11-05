// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useSyncExternalStore } from 'react';
import type { CollaborationStoreOptions } from '../CollaborationStoreOptions.js';
import type { CollaboratorInfo } from '../CollaboratorInfo.js';
import { useCollaborationClient } from './useCollaborationClient.js';

const EMPTY: CollaboratorInfo[] = [];
const noSubscription = () => () => { /* nothing to unsubscribe */ };

/**
 * Subscribes to the live remote collaborators of a room. Backed by the shared collaboration WebSocket via
 * `useSyncExternalStore`, so ONLY the components using this hook re-render on presence updates - a remote
 * cursor move never re-renders the rest of the application.
 * @param room The room to observe, when known.
 * @param options How to reach the backend; applied when this hook is the first to connect.
 * @returns The remote collaborators (excluding this session and the user's other tabs).
 */
export const useCollaborators = (room: string | undefined, options?: CollaborationStoreOptions): CollaboratorInfo[] => {
    const client = useCollaborationClient(room, options);

    // `subscribe` and `getSnapshot` MUST stay identity-stable across renders (memoized on `client` only).
    // useSyncExternalStore re-subscribes - and re-checks the snapshot for a tear - every time `subscribe`'s
    // identity changes. Against a store that changes tens of times a second (a remote collaborator dragging
    // sends presence at up to 30 Hz) that chains into an endless update loop.
    const subscribe = useCallback(
        (listener: () => void) => (client ? client.subscribe(listener) : noSubscription()),
        [client]);
    const getSnapshot = useCallback(() => (client ? client.getCollaborators() : EMPTY), [client]);

    return useSyncExternalStore(subscribe, getSnapshot, () => EMPTY);
};
