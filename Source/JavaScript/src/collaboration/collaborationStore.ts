// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CollaborationClient } from './CollaborationClient.js';
import type { CollaborationStoreOptions } from './CollaborationStoreOptions.js';
import { browserStorage } from './browserStorage.js';
import { getOrCreateAvatarColor } from './avatarColor.js';
import { createSessionId } from './createSessionId.js';
import { claimSessionId, releaseSession, vouchForSession, SESSION_HEARTBEAT_MS } from './sessionIdentity.js';

let activeClient: CollaborationClient | undefined;
let activeRoom: string | undefined;
let referenceCount = 0;
let activeSessionId: string | undefined;
let heartbeat: ReturnType<typeof setInterval> | undefined;
let unloadListenerInstalled = false;

const stopHeartbeat = () => {
    if (heartbeat) clearInterval(heartbeat);
    heartbeat = undefined;
    if (activeSessionId) releaseSession(browserStorage('local'), activeSessionId);
    activeSessionId = undefined;
};

const closeActiveClient = () => {
    activeClient?.dispose();
    activeClient = undefined;
    activeRoom = undefined;
    referenceCount = 0;
};

const leaveOnUnload = () => {
    // Close the socket explicitly: the server treats a closed socket as a leave right away instead of waiting
    // for its silence timeout. A page that is only being cached keeps its connection.
    closeActiveClient();
    stopHeartbeat();
};

const installUnloadListener = () => {
    if (unloadListenerInstalled || typeof window === 'undefined') return;
    unloadListenerInstalled = true;
    window.addEventListener('pagehide', event => {
        if (!event.persisted) leaveOnUnload();
    });
};

/**
 * Acquires the singleton {@link CollaborationClient} for a room. Reference-counted so the connection lives for
 * as long as any part of the UI needs presence, and switching rooms tears the previous connection down
 * (leaving that room) before joining the next.
 * @param room The room to collaborate in.
 * @param options How to reach the backend and which color to use. They apply when this call creates the client.
 * @returns The shared client.
 */
export const acquireCollaboration = (room: string, options: CollaborationStoreOptions = {}): CollaborationClient => {
    if (activeClient && activeRoom !== room) {
        closeActiveClient();
    }
    if (!activeClient) {
        // The id is claimed once per page and kept across rooms and reconnects: a reconnect must reuse its
        // presence, never add a second one.
        const shared = browserStorage('local');
        activeSessionId ??= claimSessionId(browserStorage('session'), shared, Date.now(), createSessionId);
        const sessionId = activeSessionId;
        heartbeat ??= setInterval(() => vouchForSession(browserStorage('local'), sessionId, Date.now()), SESSION_HEARTBEAT_MS);
        installUnloadListener();
        const color = options.color?.replace(/^#/, '') || getOrCreateAvatarColor(shared);
        activeClient = new CollaborationClient(room, sessionId, color, { path: options.path, buildUrl: options.buildUrl });
        activeRoom = room;
    }
    referenceCount++;
    return activeClient;
};

/**
 * Releases a previously acquired client; the connection closes (leaving the room) when the last holder
 * releases it.
 * @param client The client returned by {@link acquireCollaboration}.
 */
export const releaseCollaboration = (client: CollaborationClient): void => {
    if (client !== activeClient) {
        return;
    }
    referenceCount--;
    if (referenceCount <= 0) {
        closeActiveClient();
        stopHeartbeat();
    }
};

/**
 * Gets the currently active collaboration client.
 * @returns The active client, or undefined when nothing has acquired one.
 */
export const currentCollaboration = (): CollaborationClient | undefined => activeClient;
