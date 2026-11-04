// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The part an operation plays in an actor's history.
 */
export const OperationRole = {
    /** A forward operation: something the actor did. */
    forward: 'Forward',

    /** An operation that undid an earlier operation. */
    undo: 'Undo',

    /** An operation that reissued an earlier operation after it was undone. */
    redo: 'Redo'
} as const;

export type OperationRole = typeof OperationRole[keyof typeof OperationRole];
