// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CollaborationConnectionState } from './CollaborationConnectionState.js';
import type { CollaborationConnectionParameters } from './CollaborationConnectionParameters.js';
import type { CollaborationOptions } from './CollaborationOptions.js';
import type { CollaboratorInfo } from './CollaboratorInfo.js';
import { ConnectionKeepAlive } from './ConnectionKeepAlive.js';
import type { ServerMessage } from './ServerMessage.js';

/** The default path of the collaboration endpoint. */
export const DEFAULT_COLLABORATION_PATH = '/ws/collaboration';

/** How often the server pings; silence for twice this long means the connection is stale. */
const SERVER_PING_INTERVAL_MS = 20_000;

/** Minimum spacing between presence sends, so a fast-moving pointer does not flood the socket. */
const PRESENCE_SEND_INTERVAL_MS = 33;

/** How long a connection may be down before the user is told. */
const RECONNECTING_INDICATOR_DELAY_MS = 30_000;

interface Presence {
    x: number;
    y: number;
    area: string;
    surfaceId: string;
}

/**
 * A resilient two-way WebSocket client for live collaboration presence. The socket's lifetime is this
 * session's presence: connecting joins the room, disconnecting leaves it. Reconnects use bounded backoff
 * without console errors; a stale connection (no server ping within twice the server's cadence) is torn down
 * and re-established, and the server replays the current roster on every (re)join, so state always converges.
 *
 * Consumers subscribe to roster changes via {@link CollaborationClient.subscribe} and
 * {@link CollaborationClient.getCollaborators} - designed for `useSyncExternalStore`, so only the components
 * that render presence re-render on updates.
 */
export class CollaborationClient {
    readonly #collaborators = new Map<string, CollaboratorInfo>();
    readonly #listeners = new Set<() => void>();
    readonly #keepAlive: ConnectionKeepAlive;
    #reconnectAttempt = 0;
    #reconnectTimer?: ReturnType<typeof setTimeout>;
    #reconnectingIndicatorTimer?: ReturnType<typeof setTimeout>;
    #connectionState = CollaborationConnectionState.Connecting;
    #joined = false;
    #socket?: WebSocket;
    #disposed = false;
    #snapshot: CollaboratorInfo[] = [];
    #ownOtherTabs = 0;
    #selfUserId?: string;
    #lastPresenceSentAt = 0;
    #pendingPresence?: Presence;
    #latestPresence?: Presence;
    #presenceFlushTimer?: ReturnType<typeof setTimeout>;

    /**
     * Initializes a new instance of the CollaborationClient class. The client connects right away.
     * @param room The room to join.
     * @param sessionId This session's (browser tab's) identifier.
     * @param color This session's color as a hex value without the leading `#`.
     * @param options How to reach the backend.
     */
    constructor(
        readonly room: string,
        readonly sessionId: string,
        readonly color: string,
        readonly options: CollaborationOptions = {}) {
        this.#keepAlive = new ConnectionKeepAlive(
            SERVER_PING_INTERVAL_MS,
            () => this.#reconnect(),
            SERVER_PING_INTERVAL_MS * 2);
        this.#connect();
    }

    /**
     * Subscribes to roster changes.
     * @param listener Invoked whenever the collaborator set, any collaborator's state or the connection state changes.
     * @returns A function that unsubscribes.
     */
    subscribe(listener: () => void): () => void {
        this.#listeners.add(listener);
        return () => this.#listeners.delete(listener);
    }

    /**
     * Gets the current remote collaborators as a stable snapshot (identity changes only when the roster
     * actually changed - safe for `useSyncExternalStore`). Other tabs of the signed-in user are not
     * collaborators - nobody needs a cursor or a list entry for themselves - see
     * {@link CollaborationClient.getOwnOtherTabs}.
     * @returns The remote collaborators, one per session, of users other than this one.
     */
    getCollaborators(): CollaboratorInfo[] {
        return this.#snapshot;
    }

    /**
     * Gets how many other sessions (tabs, windows, devices) the signed-in user has open in this room.
     * @returns The number of the user's other sessions.
     */
    getOwnOtherTabs(): number {
        return this.#ownOtherTabs;
    }

    /**
     * Gets the state of the connection.
     * @returns The connection state.
     */
    getConnectionState(): CollaborationConnectionState {
        return this.#connectionState;
    }

    /**
     * Tells the others where this session's pointer is. Sends are throttled; the latest position always goes out.
     * @param x The pointer's horizontal position in surface content coordinates.
     * @param y The pointer's vertical position in surface content coordinates.
     * @param area The area of the application the session is in.
     * @param surfaceId The surface within the area that the session is looking at.
     */
    sendPresence(x: number, y: number, area: string, surfaceId: string): void {
        this.#latestPresence = { x, y, area, surfaceId };
        this.#pendingPresence = this.#latestPresence;
        const elapsed = performance.now() - this.#lastPresenceSentAt;
        if (elapsed >= PRESENCE_SEND_INTERVAL_MS) {
            this.#flushPresence();
        } else if (!this.#presenceFlushTimer) {
            this.#presenceFlushTimer = setTimeout(() => this.#flushPresence(), PRESENCE_SEND_INTERVAL_MS - elapsed);
        }
    }

    /**
     * Announces that this session is no longer on any surface, so the others stop drawing its cursor where it
     * last was and list it with no area while it is, say, on a settings page.
     */
    leaveArea(): void {
        this.sendPresence(0, 0, '', '');
    }

    /** Permanently closes the connection and stops reconnecting (this session leaves the room). */
    dispose(): void {
        this.#disposed = true;
        if (this.#reconnectTimer) clearTimeout(this.#reconnectTimer);
        if (this.#reconnectingIndicatorTimer) clearTimeout(this.#reconnectingIndicatorTimer);
        this.#keepAlive.stop();
        if (this.#presenceFlushTimer) clearTimeout(this.#presenceFlushTimer);
        this.#socket?.close();
        this.#socket = undefined;
    }

    #buildUrl(): string {
        const connection: CollaborationConnectionParameters = { room: this.room, sessionId: this.sessionId, color: this.color };
        if (this.options.buildUrl) {
            return this.options.buildUrl(connection);
        }

        const scheme = window.location.protocol === 'https:' ? 'wss' : 'ws';
        return `${scheme}://${window.location.host}${this.options.path ?? DEFAULT_COLLABORATION_PATH}` +
            `?room=${encodeURIComponent(connection.room)}` +
            `&sessionId=${encodeURIComponent(connection.sessionId)}` +
            `&color=${encodeURIComponent(connection.color)}`;
    }

    #flushPresence(): void {
        this.#presenceFlushTimer = undefined;
        if (!this.#joined || !this.#pendingPresence || this.#socket?.readyState !== WebSocket.OPEN) return;
        this.#lastPresenceSentAt = performance.now();
        this.#socket.send(JSON.stringify({ type: 'presence', ...this.#pendingPresence }));
        this.#pendingPresence = undefined;
    }

    #connect(): void {
        if (this.#disposed) return;

        this.#beginReconnecting();
        this.#keepAlive.start();
        let socket: WebSocket;
        try {
            socket = new WebSocket(this.#buildUrl());
        } catch {
            this.#reconnect();
            return;
        }
        this.#socket = socket;

        socket.onopen = () => {
            if (this.#disposed || this.#socket !== socket) return;

            // The upgrade succeeds before the backend joins the room. Only a roster confirms a join.
            this.#keepAlive.recordActivity();
        };

        socket.onmessage = (event: MessageEvent) => {
            if (this.#disposed || this.#socket !== socket) return;
            this.#keepAlive.recordActivity();
            let message: ServerMessage;
            try {
                message = JSON.parse(event.data);
            } catch {
                return;
            }
            this.#handle(message);
        };

        socket.onclose = socket.onerror = () => {
            if (this.#socket === socket) this.#reconnect();
        };
    }

    #beginReconnecting(): void {
        if (this.#connectionState === CollaborationConnectionState.Reconnecting || this.#reconnectingIndicatorTimer) return;
        this.#connectionState = CollaborationConnectionState.Connecting;
        this.#reconnectingIndicatorTimer = setTimeout(() => {
            this.#reconnectingIndicatorTimer = undefined;
            this.#connectionState = CollaborationConnectionState.Reconnecting;
            this.#notify();
        }, RECONNECTING_INDICATOR_DELAY_MS);
    }

    #reconnect(): void {
        if (this.#disposed) return;
        this.#keepAlive.stop();
        this.#joined = false;
        this.#pendingPresence = this.#latestPresence;
        this.#beginReconnecting();
        if (this.#socket) {
            const socket = this.#socket;
            this.#socket = undefined;
            socket.onopen = null;
            socket.onclose = null;
            socket.onmessage = null;
            socket.onerror = null;
            try {
                socket.close();
            } catch {
                // Already closed.
            }
        }

        // The roster is only trustworthy while connected - clear it so stale cursors do not linger
        // through an outage; the server replays the full roster when we rejoin.
        if (this.#collaborators.size) {
            this.#collaborators.clear();
            this.#publish();
        }
        this.#notify();
        if (this.#reconnectTimer) clearTimeout(this.#reconnectTimer);
        const delay = Math.min(500 + 500 * ++this.#reconnectAttempt, 15_000);
        this.#reconnectTimer = setTimeout(() => {
            this.#reconnectTimer = undefined;
            this.#connect();
        }, delay);
    }

    #handle(message: ServerMessage): void {
        switch (message.type) {
            case 'roster':
                this.#joined = true;
                this.#reconnectAttempt = 0;
                if (this.#reconnectingIndicatorTimer) clearTimeout(this.#reconnectingIndicatorTimer);
                this.#reconnectingIndicatorTimer = undefined;
                this.#connectionState = CollaborationConnectionState.Connected;
                this.#flushPresence();
                this.#collaborators.clear();
                this.#selfUserId = message.userId ?? this.#selfUserId;
                for (const collaborator of message.collaborators ?? []) {
                    this.#collaborators.set(collaborator.sessionId, collaborator);
                }
                this.#publish();
                break;
            case 'joined':
                if (message.collaborator) {
                    this.#collaborators.set(message.collaborator.sessionId, message.collaborator);
                    this.#publish();
                }
                break;
            case 'left':
                if (message.sessionId && this.#collaborators.delete(message.sessionId)) {
                    this.#publish();
                }
                break;
            case 'presence': {
                const existing = message.sessionId ? this.#collaborators.get(message.sessionId) : undefined;
                if (existing) {
                    const x = message.x ?? existing.x;
                    const y = message.y ?? existing.y;
                    const area = message.area ?? existing.area;
                    const surfaceId = message.surfaceId ?? existing.surfaceId;

                    // Only moving the cursor is activity (the server's rule too): a repeated position - the
                    // replay after a reconnect, a zoom - must not flip a really idle collaborator to active.
                    const moved = x !== existing.x || y !== existing.y;
                    const isIdle = moved ? false : existing.isIdle;

                    // A dragging collaborator's stream is almost never a true no-op, but skipping an
                    // occasional redundant tick still trims publishes - and every publish is a snapshot
                    // rebuild plus a re-render of every subscriber.
                    if (!moved && area === existing.area && surfaceId === existing.surfaceId && isIdle === existing.isIdle) {
                        break;
                    }
                    this.#collaborators.set(existing.sessionId, { ...existing, x, y, area, surfaceId, isIdle });
                    this.#publish();
                }
                break;
            }
            case 'idle': {
                const idleTarget = message.sessionId ? this.#collaborators.get(message.sessionId) : undefined;
                if (idleTarget && idleTarget.isIdle !== message.isIdle) {
                    this.#collaborators.set(idleTarget.sessionId, { ...idleTarget, isIdle: message.isIdle ?? false });
                    this.#publish();
                }
                break;
            }
            case 'ping':
                // Activity was already recorded; answer so intermediaries see two-way traffic.
                if (this.#socket?.readyState === WebSocket.OPEN) {
                    this.#socket.send(JSON.stringify({ type: 'pong' }));
                }
                break;
        }
    }

    #publish(): void {
        const all = [...this.#collaborators.values()];
        const others = this.#selfUserId ? all.filter(collaborator => collaborator.userId !== this.#selfUserId) : all;
        this.#ownOtherTabs = all.length - others.length;
        this.#snapshot = others;
        this.#notify();
    }

    #notify(): void {
        for (const listener of this.#listeners) {
            listener();
        }
    }
}
