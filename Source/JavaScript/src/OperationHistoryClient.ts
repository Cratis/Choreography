// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OperationHistoryItem } from './OperationHistoryItem.js';
import { OperationHistoryTransport } from './OperationHistoryTransport.js';
import { OperationOutcome } from './OperationOutcome.js';

/** Called when the history of a scope and actor may have changed. */
export type OperationHistoryChanged = () => void;

/**
 * Reads an actor's operation history and asks the backend to undo and redo, on top of a transport.
 * It holds no history of its own: the backend is authoritative, and every answer comes from it.
 */
export class OperationHistoryClient {
    readonly #listeners = new Map<string, Set<OperationHistoryChanged>>();

    /**
     * Initializes a new instance of the OperationHistoryClient class.
     * @param transport The transport that carries requests to the backend.
     */
    constructor(readonly transport: OperationHistoryTransport) {
    }

    /**
     * Gets the actor's operation history in a scope, oldest first.
     * @param scope The scope to read.
     * @param actor The actor whose history to read.
     * @param signal Aborts the request.
     * @returns The history items.
     */
    getHistory(scope: string, actor: string, signal?: AbortSignal): Promise<OperationHistoryItem[]> {
        return this.transport.getHistory(scope, actor, signal);
    }

    /**
     * Asks the backend to undo the actor's latest operation in the scope. The history is reported as changed
     * for every outcome, because a refusal can come with new eligibility information.
     * @param scope The scope.
     * @param actor The actor.
     * @param signal Aborts the request.
     * @returns What the backend did.
     */
    async undo(scope: string, actor: string, signal?: AbortSignal): Promise<OperationOutcome> {
        const outcome = await this.transport.undo(scope, actor, signal);
        this.notifyChanged(scope, actor);
        return outcome;
    }

    /**
     * Asks the backend to redo the actor's latest undone operation in the scope.
     * @param scope The scope.
     * @param actor The actor.
     * @param signal Aborts the request.
     * @returns What the backend did.
     */
    async redo(scope: string, actor: string, signal?: AbortSignal): Promise<OperationOutcome> {
        const outcome = await this.transport.redo(scope, actor, signal);
        this.notifyChanged(scope, actor);
        return outcome;
    }

    /**
     * Tells listeners that the history of a scope and actor may have changed. Call it after the application
     * performs a forward operation through its own commands, so a history view re-reads.
     * @param scope The scope that changed.
     * @param actor The actor whose history changed.
     */
    notifyChanged(scope: string, actor: string): void {
        for (const listener of [...(this.#listeners.get(this.#key(scope, actor)) ?? [])]) {
            listener();
        }
    }

    /**
     * Listens for changes to the history of a scope and actor: changes made through this client, and changes
     * the transport reports when it supports subscribing.
     * @param scope The scope to watch.
     * @param actor The actor whose history to watch.
     * @param listener Called when the history may have changed.
     * @returns A function that stops listening.
     */
    onChanged(scope: string, actor: string, listener: OperationHistoryChanged): () => void {
        const key = this.#key(scope, actor);
        let listeners = this.#listeners.get(key);
        if (!listeners) {
            listeners = new Set();
            this.#listeners.set(key, listeners);
        }
        listeners.add(listener);

        const stopTransport = this.transport.subscribe?.(scope, actor, listener);

        return () => {
            stopTransport?.();
            listeners.delete(listener);
            if (listeners.size === 0) {
                this.#listeners.delete(key);
            }
        };
    }

    #key(scope: string, actor: string): string {
        return JSON.stringify([scope, actor]);
    }
}
