// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { OperationHistoryClient } from '../OperationHistoryClient.js';
import { OperationHistoryItem } from '../OperationHistoryItem.js';
import { OperationRole } from '../OperationRole.js';
import { OperationStatus } from '../OperationStatus.js';

/**
 * Options for useOperationHistory.
 */
export interface UseOperationHistoryOptions {
    /** The client to read history through. */
    client: OperationHistoryClient;

    /** The scope to read, for example a document. */
    scope: string;

    /** The actor whose history to read. */
    actor: string;

    /** When set, the history is re-read at this interval in milliseconds. Use it when the transport cannot push. */
    pollIntervalInMilliseconds?: number;

    /** When false nothing is read. Defaults to true. */
    enabled?: boolean;
}

/**
 * The state of an actor's operation history in a scope.
 */
export interface OperationHistory {
    /** The history, oldest first. Empty until the first read completes. */
    items: OperationHistoryItem[];

    /** True until the first read for this scope and actor completes, successfully or not. */
    isLoading: boolean;

    /** True while any read is in flight. */
    isRefreshing: boolean;

    /** The error of the latest read, when it failed. The previous items are kept. */
    error?: Error;

    /** The operation an undo would reverse, with its eligibility. Undefined when there is nothing to undo. */
    nextUndo?: OperationHistoryItem;

    /** The operation a redo would reissue, with its eligibility. Undefined when there is nothing to redo. */
    nextRedo?: OperationHistoryItem;

    /** Whether an undo is currently offered. The backend still decides when it is requested. */
    canUndo: boolean;

    /** Whether a redo is currently offered. The backend still decides when it is requested. */
    canRedo: boolean;

    /** Re-reads the history. A failure is reported in error, not thrown. */
    refresh(): Promise<void>;
}

const asError = (error: unknown): Error => error instanceof Error ? error : new Error(String(error));

/**
 * Reads an actor's operation history in a scope, and keeps it current: it re-reads when an undo or a redo
 * goes through the same client, when the transport reports a change, and on an optional interval.
 * A response that arrives after the scope or actor changed, or after unmount, is ignored.
 * @param options What to read.
 * @returns The history state.
 */
export const useOperationHistory = (options: UseOperationHistoryOptions): OperationHistory => {
    const { client, scope, actor, pollIntervalInMilliseconds, enabled = true } = options;
    const [items, setItems] = useState<OperationHistoryItem[]>([]);
    const [isLoading, setIsLoading] = useState(enabled);
    const [inFlight, setInFlight] = useState(0);
    const [error, setError] = useState<Error | undefined>();
    const generation = useRef(0);
    const refreshRef = useRef<() => Promise<void>>(async () => undefined);

    useEffect(() => {
        generation.current++;
        const current = generation.current;
        const controller = new AbortController();
        setItems([]);
        setError(undefined);
        setIsLoading(enabled);

        const read = async () => {
            if (!enabled) return;
            setInFlight(count => count + 1);
            try {
                const history = await client.getHistory(scope, actor, controller.signal);
                if (generation.current !== current) return;
                setItems(history);
                setError(undefined);
            } catch (readError) {
                if (generation.current !== current) return;
                setError(asError(readError));
            } finally {
                if (generation.current === current) {
                    setIsLoading(false);
                    setInFlight(count => count - 1);
                }
            }
        };

        refreshRef.current = read;
        if (!enabled) {
            return undefined;
        }

        void read();
        const stopListening = client.onChanged(scope, actor, () => void read());
        const interval = pollIntervalInMilliseconds ? setInterval(() => void read(), pollIntervalInMilliseconds) : undefined;

        return () => {
            generation.current++;
            controller.abort();
            stopListening();
            if (interval) clearInterval(interval);
        };
    }, [client, scope, actor, enabled, pollIntervalInMilliseconds]);

    const refresh = useCallback(() => refreshRef.current(), []);

    return useMemo(() => {
        // The backend marks the next undo target (a completed, non-undo entry) and the next redo target (an
        // undone entry), and puts the reason on whichever of them is blocked.
        const nextUndo = items.find(item => item.canUndo || (item.blockedReason !== undefined && item.status === OperationStatus.completed && item.role !== OperationRole.undo));
        const nextRedo = items.find(item => item.canRedo || (item.blockedReason !== undefined && item.status === OperationStatus.undone));
        return {
            items,
            isLoading,
            isRefreshing: inFlight > 0,
            error,
            nextUndo,
            nextRedo,
            canUndo: nextUndo?.canUndo === true,
            canRedo: nextRedo?.canRedo === true,
            refresh
        };
    }, [items, isLoading, inFlight, error, refresh]);
};
