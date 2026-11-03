// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OperationAction, UseOperationActionOptions, useOperationAction } from './useOperationAction.js';

/**
 * Undoes the actor's latest operation in a scope, with pending, outcome and error state.
 * The backend decides: a conflicting later change comes back as a Blocked outcome with the reason, and the
 * latest operation is never skipped to reach an older one.
 * @param options What to undo.
 * @returns The undo action.
 */
export const useUndo = ({ client, scope, actor, onOutcome }: UseOperationActionOptions): OperationAction =>
    useOperationAction(() => client.undo(scope, actor), onOutcome);
