// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderHook, act } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { OperationHistoryClient } from '../../OperationHistoryClient.js';
import { FakeTransport } from '../../testing.js';
import { useRedo } from '../useRedo.js';

describe('when redoing', () => {
    it('should send a redo request, not an undo, and report the outcome', async () => {
        const transport = new FakeTransport();
        transport.redoOutcome = { kind: 'Blocked', reason: 'No longer valid' };
        const { result } = renderHook(() => useRedo({ client: new OperationHistoryClient(transport), scope: 'doc-1', actor: 'alice' }));

        await act(() => result.current.execute());

        expect(transport.redoRequests).toEqual([{ scope: 'doc-1', actor: 'alice' }]);
        expect(transport.undoRequests.length).toBe(0);
        expect(result.current.outcome).toEqual({ kind: 'Blocked', reason: 'No longer valid' });
    });
});
