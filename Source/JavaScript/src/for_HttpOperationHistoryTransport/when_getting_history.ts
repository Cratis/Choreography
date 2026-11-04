// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { createHttpTransport } from '../HttpOperationHistoryTransport.js';
import { OperationHistoryRequestFailed } from '../OperationHistoryRequestFailed.js';

const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });

const rawItem = {
    id: 'op-1', position: 1, scope: 'doc 1', actor: 'alice', kind: 'RenameModule', role: 'forward', status: 'completed',
    eventSequenceNumbers: [4, 5], started: '2026-01-01T00:00:00Z', target: null, canUndo: true, canRedo: false, blockedReason: null
};

describe('when getting history over http', () => {
    it('should request the history route with an encoded scope and actor', async () => {
        const urls: string[] = [];
        const transport = createHttpTransport({ baseUrl: '/api/operations/', fetch: (async (url: string) => { urls.push(url); return json([]); }) as typeof fetch });

        await transport.getHistory('doc 1', 'alice');

        expect(urls).toEqual(['/api/operations/history?scope=doc+1&actor=alice']);
    });

    it('should read the items', async () => {
        const transport = createHttpTransport({ baseUrl: '/api', fetch: (async () => json([rawItem])) as typeof fetch });

        const [item] = await transport.getHistory('doc 1', 'alice');

        expect(item).toMatchObject({ id: 'op-1', role: 'Forward', status: 'Completed', eventSequenceNumbers: [4, 5], canUndo: true, canRedo: false, target: undefined, blockedReason: undefined });
        expect(item.started.toISOString()).toBe('2026-01-01T00:00:00.000Z');
    });

    it('should send the configured headers', async () => {
        let sent: Headers | undefined;
        const transport = createHttpTransport({
            baseUrl: '/api',
            headers: async () => ({ Authorization: 'Bearer token' }),
            fetch: (async (_url: string, init: RequestInit) => { sent = init.headers as Headers; return json([]); }) as typeof fetch
        });

        await transport.getHistory('s', 'a');

        expect(sent?.get('Authorization')).toBe('Bearer token');
    });

    it('should fail with the status when the host answers with an error', async () => {
        const transport = createHttpTransport({ baseUrl: '/api', fetch: (async () => json({}, 500)) as typeof fetch });

        const failure = await transport.getHistory('s', 'a').catch(error => error);

        expect(failure).toBeInstanceOf(OperationHistoryRequestFailed);
        expect(failure.status).toBe(500);
    });

    it('should fail when the request cannot be sent', async () => {
        const transport = createHttpTransport({ baseUrl: '/api', fetch: (async () => { throw new TypeError('offline'); }) as typeof fetch });

        await expect(transport.getHistory('s', 'a')).rejects.toBeInstanceOf(OperationHistoryRequestFailed);
    });

    it('should reject a response that is not a list', async () => {
        const transport = createHttpTransport({ baseUrl: '/api', fetch: (async () => json({ nope: true })) as typeof fetch });

        await expect(transport.getHistory('s', 'a')).rejects.toBeInstanceOf(OperationHistoryRequestFailed);
    });

    it('should reject an item with an unknown status', async () => {
        const transport = createHttpTransport({ baseUrl: '/api', fetch: (async () => json([{ ...rawItem, status: 'Exploded' }])) as typeof fetch });

        await expect(transport.getHistory('s', 'a')).rejects.toBeInstanceOf(OperationHistoryRequestFailed);
    });
});
