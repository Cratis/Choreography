// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { renderHook, waitFor, act } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { OperationHistoryClient } from '../../OperationHistoryClient.js';
import { FakeTransport, gate, historyItem } from '../../testing.js';
import { useOperationHistory } from '../useOperationHistory.js';

const setup = (transport: FakeTransport, scope = 'doc-1', actor = 'alice') => {
    const client = new OperationHistoryClient(transport);
    const hook = renderHook(({ scope, actor }) => useOperationHistory({ client, scope, actor }), { initialProps: { scope, actor } });
    return { client, ...hook };
};

describe('when reading history', () => {
    it('should be loading until the first read completes, then show the items', async () => {
        const transport = new FakeTransport();
        transport.history = [historyItem({ id: 'a' })];
        const { result } = setup(transport);

        expect(result.current.isLoading).toBe(true);
        await waitFor(() => expect(result.current.isLoading).toBe(false));

        expect(result.current.items.map(item => item.id)).toEqual(['a']);
    });

    it('should expose the next undo target and offer undo when it is eligible', async () => {
        const transport = new FakeTransport();
        transport.history = [historyItem({ id: 'a', status: 'Undone', canRedo: true }), historyItem({ id: 'b', position: 2, canUndo: true })];
        const { result } = setup(transport);

        await waitFor(() => expect(result.current.isLoading).toBe(false));

        expect(result.current.nextUndo?.id).toBe('b');
        expect(result.current.canUndo).toBe(true);
        expect(result.current.nextRedo?.id).toBe('a');
        expect(result.current.canRedo).toBe(true);
    });

    it('should show a blocked undo target with its reason and not offer undo', async () => {
        const transport = new FakeTransport();
        transport.history = [historyItem({ id: 'a' }), historyItem({ id: 'b', position: 2, blockedReason: 'Changed by someone else' })];
        const { result } = setup(transport);

        await waitFor(() => expect(result.current.isLoading).toBe(false));

        expect(result.current.nextUndo?.blockedReason).toBe('Changed by someone else');
        expect(result.current.canUndo).toBe(false);
    });

    it('should show a blocked redo target with its reason and not offer redo', async () => {
        const transport = new FakeTransport();
        transport.history = [historyItem({ id: 'a', status: 'Undone', blockedReason: 'No longer valid' })];
        const { result } = setup(transport);

        await waitFor(() => expect(result.current.isLoading).toBe(false));

        expect(result.current.nextRedo?.blockedReason).toBe('No longer valid');
        expect(result.current.canRedo).toBe(false);
        expect(result.current.nextUndo).toBeUndefined();
    });

    it('should keep the previous items and report the error when a refresh fails', async () => {
        const transport = new FakeTransport();
        transport.history = [historyItem({ id: 'a' })];
        const { result } = setup(transport);
        await waitFor(() => expect(result.current.isLoading).toBe(false));

        transport.failure = new Error('offline');
        await act(() => result.current.refresh());

        expect(result.current.error?.message).toBe('offline');
        expect(result.current.items.map(item => item.id)).toEqual(['a']);
    });

    it('should re-read after an undo goes through the same client', async () => {
        const transport = new FakeTransport();
        transport.history = [historyItem({ id: 'a', canUndo: true })];
        const { result, client } = setup(transport);
        await waitFor(() => expect(result.current.canUndo).toBe(true));

        transport.history = [historyItem({ id: 'a', status: 'Undone', canRedo: true })];
        await act(() => client.undo('doc-1', 'alice'));

        await waitFor(() => expect(result.current.canRedo).toBe(true));
        expect(result.current.canUndo).toBe(false);
    });

    it('should re-read when the transport reports a change', async () => {
        const transport = new FakeTransport();
        const { result } = setup(transport);
        await waitFor(() => expect(result.current.isLoading).toBe(false));

        transport.history = [historyItem({ id: 'pushed' })];
        act(() => transport.subscribers.forEach(subscriber => subscriber()));

        await waitFor(() => expect(result.current.items.map(item => item.id)).toEqual(['pushed']));
    });

    it('should ignore a response that arrives after the scope changed', async () => {
        const transport = new FakeTransport();
        const slow = gate();
        transport.historyGate = slow.promise;
        transport.history = [historyItem({ id: 'old-scope' })];
        const { result, rerender } = setup(transport, 'doc-1');

        transport.historyGate = undefined;
        transport.history = [historyItem({ id: 'new-scope', scope: 'doc-2' })];
        rerender({ scope: 'doc-2', actor: 'alice' });
        await waitFor(() => expect(result.current.items.map(item => item.id)).toEqual(['new-scope']));

        slow.open();
        await new Promise(resolve => setTimeout(resolve, 10));

        expect(result.current.items.map(item => item.id)).toEqual(['new-scope']);
    });

    it('should read nothing and stop listening when disabled', async () => {
        const transport = new FakeTransport();
        const client = new OperationHistoryClient(transport);

        const { result } = renderHook(() => useOperationHistory({ client, scope: 's', actor: 'a', enabled: false }));

        expect(result.current.isLoading).toBe(false);
        expect(transport.historyRequests.length).toBe(0);
        expect(transport.subscribers.length).toBe(0);
    });

    it('should stop listening when unmounted', async () => {
        const transport = new FakeTransport();
        const { result, unmount } = setup(transport);
        await waitFor(() => expect(result.current.isLoading).toBe(false));
        expect(transport.subscribers.length).toBe(1);

        unmount();

        expect(transport.subscribers.length).toBe(0);
    });
});
