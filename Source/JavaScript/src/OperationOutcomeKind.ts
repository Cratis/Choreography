// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * What happened when an operation, an undo or a redo was requested.
 */
export const OperationOutcomeKind = {
    /** The request was carried out. */
    succeeded: 'Succeeded',

    /** The request was refused because current state does not allow it; the reason says why. */
    blocked: 'Blocked',

    /** The actor has nothing to undo in the scope. */
    nothingToUndo: 'NothingToUndo',

    /** The actor has nothing to redo in the scope. */
    nothingToRedo: 'NothingToRedo',

    /** The request was attempted and failed. */
    failed: 'Failed'
} as const;

export type OperationOutcomeKind = typeof OperationOutcomeKind[keyof typeof OperationOutcomeKind];
