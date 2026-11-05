// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useState } from 'react';
import { acquireCollaboration, releaseCollaboration } from '../collaborationStore.js';
import type { CollaborationClient } from '../CollaborationClient.js';
import type { CollaborationStoreOptions } from '../CollaborationStoreOptions.js';

/**
 * Holds the shared collaboration client of a room for as long as the component is mounted. The client is
 * acquired in an effect and released on cleanup, so a component that is mounted twice in development, or
 * rendered and discarded, never leaves a connection open.
 * @param room The room, or undefined when it is not known yet.
 * @param options How to reach the backend; applied when this acquisition creates the client.
 * @returns The client, or undefined before the first effect has run or while there is no room.
 */
export const useCollaborationClient = (room: string | undefined, options?: CollaborationStoreOptions): CollaborationClient | undefined => {
    const [client, setClient] = useState<CollaborationClient | undefined>();

    useEffect(() => {
        if (!room) {
            setClient(undefined);
            return undefined;
        }

        const acquired = acquireCollaboration(room, options);
        setClient(acquired);
        return () => {
            releaseCollaboration(acquired);
            setClient(undefined);
        };
        // The options only matter to the acquisition that creates the connection.
    }, [room]);

    return client;
};
