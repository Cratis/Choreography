// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OperationOutcomeKind } from './OperationOutcomeKind.js';

/**
 * The result of an undo or a redo request. The backend is authoritative: a request the page offered
 * can still come back blocked.
 */
export interface OperationOutcome {
    /** What happened. */
    kind: OperationOutcomeKind;

    /** The operation that carried the request out, when it was. */
    operationId?: string;

    /** Why the request was blocked or failed. */
    reason?: string;
}

/**
 * Whether the outcome says the request was carried out.
 * @param outcome The outcome to look at.
 * @returns True when the request succeeded.
 */
export const isSucceeded = (outcome: OperationOutcome): boolean => outcome.kind === OperationOutcomeKind.succeeded;

/**
 * Whether the outcome says the backend refused the request because of the current state, rather than failing.
 * @param outcome The outcome to look at.
 * @returns True when the request was blocked.
 */
export const isBlocked = (outcome: OperationOutcome): boolean => outcome.kind === OperationOutcomeKind.blocked;
