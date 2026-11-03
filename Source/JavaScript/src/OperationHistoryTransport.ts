// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OperationHistoryItem } from './OperationHistoryItem.js';
import { OperationOutcome } from './OperationOutcome.js';

/**
 * Carries operation history requests to the backend. Implement it for the transport the application has;
 * the package ships an HTTP one in createHttpTransport.
 */
export interface OperationHistoryTransport {
    /**
     * Gets the actor's operation history in a scope, oldest first.
     * @param scope The scope to read.
     * @param actor The actor whose history to read.
     * @param signal Aborts the request.
     */
    getHistory(scope: string, actor: string, signal?: AbortSignal): Promise<OperationHistoryItem[]>;

    /**
     * Asks the backend to undo the actor's latest operation in the scope.
     * @param scope The scope.
     * @param actor The actor.
     * @param signal Aborts the request.
     */
    undo(scope: string, actor: string, signal?: AbortSignal): Promise<OperationOutcome>;

    /**
     * Asks the backend to redo the actor's latest undone operation in the scope.
     * @param scope The scope.
     * @param actor The actor.
     * @param signal Aborts the request.
     */
    redo(scope: string, actor: string, signal?: AbortSignal): Promise<OperationOutcome>;

    /**
     * Optionally tells the client that the history of a scope changed somewhere else, so it can re-read it.
     * Implement it with whatever push channel the application has, such as server-sent events or a socket.
     * @param scope The scope to watch.
     * @param actor The actor whose history to watch.
     * @param onChanged Called when the history may have changed.
     * @returns A function that stops watching.
     */
    subscribe?(scope: string, actor: string, onChanged: () => void): () => void;
}
