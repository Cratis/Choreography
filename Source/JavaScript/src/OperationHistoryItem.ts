// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OperationRole } from './OperationRole.js';
import { OperationStatus } from './OperationStatus.js';

/**
 * One logical operation in an actor's history, as the backend reports it.
 * An operation is never inferred from individual events: the event sequence numbers are the references
 * the backend recorded for it, and say nothing about whether undoing it is safe.
 */
export interface OperationHistoryItem {
    /** The identity of the operation. */
    id: string;

    /** The position of the operation in the history. Entries are ordered oldest first. */
    position: number;

    /** The scope the operation happened in, for example a document. */
    scope: string;

    /** The actor that performed the operation. */
    actor: string;

    /** The kind of operation. */
    kind: string;

    /** Whether this is a forward operation, an undo or a redo. */
    role: OperationRole;

    /** The lifecycle status of the operation. */
    status: OperationStatus;

    /** The event sequence numbers the operation produced. May be empty. */
    eventSequenceNumbers: number[];

    /** When the operation started. */
    started: Date;

    /** For an undo or a redo, the operation it targets. */
    target?: string;

    /** Whether an undo would reverse this operation right now. Only the operation an undo would target can be true. */
    canUndo: boolean;

    /** Whether a redo would reissue this operation right now. */
    canRedo: boolean;

    /** Why the operation cannot be undone or redone, when it is the next target and is blocked. */
    blockedReason?: string;
}
