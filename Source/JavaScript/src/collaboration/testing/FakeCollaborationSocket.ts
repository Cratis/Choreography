// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { vi } from 'vitest';

/**
 * A stand-in for the browser `WebSocket`. It records what the client sends and lets a spec simulate what
 * the server delivers by invoking the handlers the client wires on.
 */
export class FakeCollaborationSocket {
    static readonly OPEN = 1;
    static instances: FakeCollaborationSocket[] = [];

    readyState = FakeCollaborationSocket.OPEN;
    onopen: (() => void) | null = null;
    onmessage: ((event: { data: string }) => void) | null = null;
    onclose: (() => void) | null = null;
    onerror: (() => void) | null = null;
    readonly send = vi.fn((_data: string) => undefined);
    readonly close = vi.fn(() => { this.readyState = 3; });

    constructor(readonly url: string) {
        FakeCollaborationSocket.instances.push(this);
    }

    /** The messages the client sent, parsed. */
    get sentMessages(): { type: string; [key: string]: unknown }[] {
        return this.send.mock.calls.map(([data]) => JSON.parse(data));
    }

    /** The most recently created socket. */
    static get latest(): FakeCollaborationSocket {
        return FakeCollaborationSocket.instances[FakeCollaborationSocket.instances.length - 1];
    }

    /**
     * Simulates the server delivering a message.
     * @param message The message.
     */
    deliver(message: unknown): void {
        this.onmessage?.({ data: JSON.stringify(message) });
    }

    /** Forgets every socket created so far. */
    static reset(): void {
        FakeCollaborationSocket.instances = [];
    }
}
