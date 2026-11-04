// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OperationHistoryItem } from './OperationHistoryItem.js';
import { OperationHistoryTransport } from './OperationHistoryTransport.js';
import { OperationOutcome } from './OperationOutcome.js';

/**
 * Builds a history item for specs, overriding only what the spec cares about.
 * @param overrides The values to set.
 * @returns A history item.
 */
export const historyItem = (overrides: Partial<OperationHistoryItem> = {}): OperationHistoryItem => ({
    id: 'op-1',
    position: 1,
    scope: 'doc-1',
    actor: 'alice',
    kind: 'RenameModule',
    role: 'Forward',
    status: 'Completed',
    eventSequenceNumbers: [10],
    started: new Date('2026-01-01T00:00:00Z'),
    canUndo: false,
    canRedo: false,
    ...overrides
});

/**
 * A transport whose answers a spec controls, and which records what it was asked.
 */
export class FakeTransport implements OperationHistoryTransport {
    history: OperationHistoryItem[] = [];
    undoOutcome: OperationOutcome = { kind: 'Succeeded', operationId: 'undo-1' };
    redoOutcome: OperationOutcome = { kind: 'Succeeded', operationId: 'redo-1' };
    historyRequests: { scope: string; actor: string }[] = [];
    undoRequests: { scope: string; actor: string }[] = [];
    redoRequests: { scope: string; actor: string }[] = [];
    historyGate?: Promise<void>;
    undoGate?: Promise<void>;
    failure?: Error;
    subscribers: (() => void)[] = [];

    async getHistory(scope: string, actor: string): Promise<OperationHistoryItem[]> {
        this.historyRequests.push({ scope, actor });
        if (this.historyGate) await this.historyGate;
        if (this.failure) throw this.failure;
        return this.history;
    }

    async undo(scope: string, actor: string): Promise<OperationOutcome> {
        this.undoRequests.push({ scope, actor });
        if (this.undoGate) await this.undoGate;
        if (this.failure) throw this.failure;
        return this.undoOutcome;
    }

    async redo(scope: string, actor: string): Promise<OperationOutcome> {
        this.redoRequests.push({ scope, actor });
        if (this.failure) throw this.failure;
        return this.redoOutcome;
    }

    subscribe(_scope: string, _actor: string, onChanged: () => void): () => void {
        this.subscribers.push(onChanged);
        return () => {
            this.subscribers = this.subscribers.filter(subscriber => subscriber !== onChanged);
        };
    }
}

/**
 * Creates a promise a spec can resolve later.
 * @returns The promise and the function that resolves it.
 */
export const gate = (): { promise: Promise<void>; open: () => void } => {
    let open!: () => void;
    const promise = new Promise<void>(resolve => {
        open = resolve;
    });
    return { promise, open };
};
