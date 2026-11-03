// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OperationAction, UseOperationActionOptions, useOperationAction } from './useOperationAction.js';

/**
 * Redoes the actor's latest undone operation in a scope, with pending, outcome and error state.
 * The backend decides: a redo that is no longer valid comes back as a Blocked outcome with the reason.
 * @param options What to redo.
 * @returns The redo action.
 */
export const useRedo = ({ client, scope, actor, onOutcome }: UseOperationActionOptions): OperationAction =>
    useOperationAction(() => client.redo(scope, actor), onOutcome);
