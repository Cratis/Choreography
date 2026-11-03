// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { createHttpTransport } from '../HttpOperationHistoryTransport.js';
import { OperationHistoryRequestFailed } from '../OperationHistoryRequestFailed.js';

const json = (body: unknown, status = 200) => new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });

describe('when undoing over http', () => {
    it('should post the scope and actor to the undo route', async () => {
        const calls: { url: string; method?: string; body?: string }[] = [];
        const transport = createHttpTransport({
            baseUrl: '/api',
            fetch: (async (url: string, init: RequestInit) => { calls.push({ url, method: init.method, body: init.body as string }); return json({ kind: 'Succeeded', operationId: 'u1' }); }) as typeof fetch
        });

        const outcome = await transport.undo('doc-1', 'alice');

        expect(calls).toEqual([{ url: '/api/undo', method: 'POST', body: JSON.stringify({ scope: 'doc-1', actor: 'alice' }) }]);
        expect(outcome).toEqual({ kind: 'Succeeded', operationId: 'u1', reason: undefined });
    });

    it('should report a refusal as a blocked outcome, not an error', async () => {
        const transport = createHttpTransport({ baseUrl: '/api', fetch: (async () => json({ kind: 'blocked', reason: 'Someone else changed it' })) as typeof fetch });

        const outcome = await transport.undo('doc-1', 'alice');

        expect(outcome).toMatchObject({ kind: 'Blocked', reason: 'Someone else changed it' });
    });

    it('should post to the redo route when redoing', async () => {
        const urls: string[] = [];
        const transport = createHttpTransport({ baseUrl: '/api', fetch: (async (url: string) => { urls.push(url); return json({ kind: 'NothingToRedo' }); }) as typeof fetch });

        const outcome = await transport.redo('doc-1', 'alice');

        expect(urls).toEqual(['/api/redo']);
        expect(outcome.kind).toBe('NothingToRedo');
    });

    it('should fail when the outcome is not understood', async () => {
        const transport = createHttpTransport({ baseUrl: '/api', fetch: (async () => json({ kind: 'Whatever' })) as typeof fetch });

        await expect(transport.undo('s', 'a')).rejects.toBeInstanceOf(OperationHistoryRequestFailed);
    });
});
