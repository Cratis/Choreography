// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The lifecycle status of an operation in the history.
 */
export const OperationStatus = {
    /** The operation has started and is not finished. */
    started: 'Started',

    /** The operation finished and is the current effect of what the actor did. */
    completed: 'Completed',

    /** The operation failed. */
    failed: 'Failed',

    /** The operation was undone by a later undo operation. */
    undone: 'Undone',

    /** The operation was undone and then reissued by a redo operation. */
    redone: 'Redone',

    /** The operation was undone, and can no longer be redone because the actor did something new. */
    discarded: 'Discarded'
} as const;

export type OperationStatus = typeof OperationStatus[keyof typeof OperationStatus];
