// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useRef, useState } from 'react';
import { OperationHistoryClient } from '../OperationHistoryClient.js';
import { OperationOutcome } from '../OperationOutcome.js';

/**
 * Options for useUndo and useRedo.
 */
export interface UseOperationActionOptions {
    /** The client to send the request through. */
    client: OperationHistoryClient;

    /** The scope to act in. */
    scope: string;

    /** The actor acting. */
    actor: string;

    /** Called with the outcome when the backend answered, including when it refused. */
    onOutcome?: (outcome: OperationOutcome) => void;
}

/**
 * The state of an undo or a redo.
 */
export interface OperationAction {
    /**
     * Sends the request. Resolves with the backend's outcome, or undefined when the request could not be completed;
     * the failure is in error. A request already in flight is not sent twice; its result is returned instead.
     */
    execute(): Promise<OperationOutcome | undefined>;

    /** True while the request is in flight. */
    isPending: boolean;

    /** The outcome of the latest request, including a blocked one. */
    outcome?: OperationOutcome;

    /** Why the latest request could not be completed. Not set when the backend answered, even with a refusal. */
    error?: Error;

    /** Clears the outcome and the error, for example when the user dismisses a message. */
    reset(): void;
}

const asError = (error: unknown): Error => error instanceof Error ? error : new Error(String(error));

/**
 * Shared implementation of useUndo and useRedo.
 * @param send Sends the request.
 * @param onOutcome Called with the outcome when the backend answered.
 * @returns The action state.
 */
export const useOperationAction = (
    send: () => Promise<OperationOutcome>,
    onOutcome?: (outcome: OperationOutcome) => void
): OperationAction => {
    const [isPending, setIsPending] = useState(false);
    const [outcome, setOutcome] = useState<OperationOutcome | undefined>();
    const [error, setError] = useState<Error | undefined>();
    const inFlight = useRef<Promise<OperationOutcome | undefined> | undefined>(undefined);
    const isMounted = useRef(true);
    const sendRef = useRef(send);
    const onOutcomeRef = useRef(onOutcome);
    sendRef.current = send;
    onOutcomeRef.current = onOutcome;

    useEffect(() => {
        isMounted.current = true;
        return () => {
            isMounted.current = false;
        };
    }, []);

    const execute = useCallback((): Promise<OperationOutcome | undefined> => {
        if (inFlight.current) return inFlight.current;

        setIsPending(true);
        setError(undefined);
        const request = (async () => {
            try {
                const result = await sendRef.current();
                if (isMounted.current) setOutcome(result);
                onOutcomeRef.current?.(result);
                return result;
            } catch (failure) {
                if (isMounted.current) {
                    setOutcome(undefined);
                    setError(asError(failure));
                }
                return undefined;
            } finally {
                inFlight.current = undefined;
                if (isMounted.current) setIsPending(false);
            }
        })();
        inFlight.current = request;
        return request;
    }, []);

    const reset = useCallback(() => {
        setOutcome(undefined);
        setError(undefined);
    }, []);

    return { execute, isPending, outcome, error, reset };
};
