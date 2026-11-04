// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderHook, waitFor, act } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { OperationHistoryClient } from '../../OperationHistoryClient.js';
import { OperationOutcome } from '../../OperationOutcome.js';
import { FakeTransport, gate } from '../../testing.js';
import { useUndo } from '../useUndo.js';

describe('when undoing', () => {
    it('should send the request for the scope and actor and report the outcome', async () => {
        const transport = new FakeTransport();
        const outcomes: OperationOutcome[] = [];
        const { result } = renderHook(() => useUndo({ client: new OperationHistoryClient(transport), scope: 'doc-1', actor: 'alice', onOutcome: outcome => outcomes.push(outcome) }));

        await act(() => result.current.execute());

        expect(transport.undoRequests).toEqual([{ scope: 'doc-1', actor: 'alice' }]);
        expect(result.current.outcome?.kind).toBe('Succeeded');
        expect(outcomes.length).toBe(1);
    });

    it('should surface a blocked outcome with its reason, without an error', async () => {
        const transport = new FakeTransport();
        transport.undoOutcome = { kind: 'Blocked', reason: 'Someone else changed it' };
        const { result } = renderHook(() => useUndo({ client: new OperationHistoryClient(transport), scope: 'doc-1', actor: 'alice' }));

        await act(() => result.current.execute());

        expect(result.current.outcome).toEqual({ kind: 'Blocked', reason: 'Someone else changed it' });
        expect(result.current.error).toBeUndefined();
    });

    it('should report a failed request as an error and clear the outcome', async () => {
        const transport = new FakeTransport();
        transport.failure = new Error('offline');
        const { result } = renderHook(() => useUndo({ client: new OperationHistoryClient(transport), scope: 'doc-1', actor: 'alice' }));

        const returned = await act(() => result.current.execute());

        expect(returned).toBeUndefined();
        expect(result.current.error?.message).toBe('offline');
        expect(result.current.outcome).toBeUndefined();
    });

    it('should be pending while in flight and not send twice', async () => {
        const transport = new FakeTransport();
        const slow = gate();
        transport.undoGate = slow.promise;
        const { result } = renderHook(() => useUndo({ client: new OperationHistoryClient(transport), scope: 'doc-1', actor: 'alice' }));

        let first!: Promise<unknown>;
        let second!: Promise<unknown>;
        act(() => {
            first = result.current.execute();
            second = result.current.execute();
        });
        await waitFor(() => expect(result.current.isPending).toBe(true));
        slow.open();
        await act(async () => { await Promise.all([first, second]); });

        expect(transport.undoRequests.length).toBe(1);
        expect(result.current.isPending).toBe(false);
    });

    it('should clear the outcome when reset', async () => {
        const { result } = renderHook(() => useUndo({ client: new OperationHistoryClient(new FakeTransport()), scope: 's', actor: 'a' }));
        await act(() => result.current.execute());

        act(() => result.current.reset());

        expect(result.current.outcome).toBeUndefined();
    });
});
