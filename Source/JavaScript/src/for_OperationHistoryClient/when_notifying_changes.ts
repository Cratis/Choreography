// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { OperationHistoryClient } from '../OperationHistoryClient.js';
import { FakeTransport } from '../testing.js';

describe('when the history changes', () => {
    it('should notify listeners of the same scope and actor after an undo, even when it is blocked', async () => {
        const transport = new FakeTransport();
        transport.undoOutcome = { kind: 'Blocked', reason: 'Conflict' };
        const client = new OperationHistoryClient(transport);
        let calls = 0;
        client.onChanged('doc-1', 'alice', () => calls++);

        await client.undo('doc-1', 'alice');

        expect(calls).toBe(1);
    });

    it('should notify after a redo', async () => {
        const client = new OperationHistoryClient(new FakeTransport());
        let calls = 0;
        client.onChanged('doc-1', 'alice', () => calls++);

        await client.redo('doc-1', 'alice');

        expect(calls).toBe(1);
    });

    it('should not notify listeners of another scope or actor', async () => {
        const client = new OperationHistoryClient(new FakeTransport());
        let calls = 0;
        client.onChanged('doc-2', 'alice', () => calls++);
        client.onChanged('doc-1', 'bob', () => calls++);

        await client.undo('doc-1', 'alice');

        expect(calls).toBe(0);
    });

    it('should not notify when the request fails', async () => {
        const transport = new FakeTransport();
        transport.failure = new Error('offline');
        const client = new OperationHistoryClient(transport);
        let calls = 0;
        client.onChanged('doc-1', 'alice', () => calls++);

        await client.undo('doc-1', 'alice').catch(() => undefined);

        expect(calls).toBe(0);
    });

    it('should stop notifying and stop the transport subscription when unsubscribed', () => {
        const transport = new FakeTransport();
        const client = new OperationHistoryClient(transport);
        let calls = 0;
        const stop = client.onChanged('doc-1', 'alice', () => calls++);
        expect(transport.subscribers.length).toBe(1);

        stop();
        client.notifyChanged('doc-1', 'alice');

        expect(calls).toBe(0);
        expect(transport.subscribers.length).toBe(0);
    });
});
