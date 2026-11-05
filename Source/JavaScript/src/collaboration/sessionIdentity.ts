// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const SESSION_KEY = 'collaboration:sessionId';
const ALIVE_PREFIX = 'collaboration:alive:';

/** How often a live page vouches for its session id in the storage shared by the browser's tabs. */
export const SESSION_HEARTBEAT_MS = 2_000;

/** A session id vouched for within this window belongs to a page that is still open. */
const ALIVE_WINDOW_MS = SESSION_HEARTBEAT_MS * 3;

/** Vouches older than this are leftovers of crashed pages and are swept. */
const SWEEP_AFTER_MS = 60 * 60 * 1000;

/**
 * Claims the collaboration session id of this page. The id lives in `sessionStorage`, so a reload reuses it and
 * the server replaces the old connection instead of listing the same tab twice. A duplicated tab copies
 * `sessionStorage` including the id, though, and two live pages sharing an id evict each other in an endless
 * reconnect loop: every page therefore vouches for its id in the shared storage, and a page that finds its id
 * vouched for by another live page mints its own.
 * @param session The storage of this tab.
 * @param shared The storage shared by every tab of the browser.
 * @param now The current time in epoch milliseconds.
 * @param create Creates a new session id.
 * @returns The session id this page uses.
 */
export const claimSessionId = (session: Storage, shared: Storage, now: number, create: () => string): string => {
    sweepStaleVouches(shared, now);

    let id = session.getItem(SESSION_KEY) ?? undefined;
    if (id && isVouchedFor(shared, id, now)) {
        id = undefined;
    }

    if (!id) {
        id = create();
        session.setItem(SESSION_KEY, id);
    }

    vouchForSession(shared, id, now);
    return id;
};

/**
 * Vouches that the page holding the session id is still open.
 * @param shared The storage shared by every tab of the browser.
 * @param id The session id.
 * @param now The current time in epoch milliseconds.
 */
export const vouchForSession = (shared: Storage, id: string, now: number) => shared.setItem(ALIVE_PREFIX + id, now.toString());

/**
 * Withdraws the vouch: the page is going away, so a reload may take the session id over right away.
 * @param shared The storage shared by every tab of the browser.
 * @param id The session id.
 */
export const releaseSession = (shared: Storage, id: string) => shared.removeItem(ALIVE_PREFIX + id);

const isVouchedFor = (shared: Storage, id: string, now: number): boolean => {
    const vouched = Number(shared.getItem(ALIVE_PREFIX + id));
    return Number.isFinite(vouched) && vouched > 0 && now - vouched < ALIVE_WINDOW_MS;
};

const sweepStaleVouches = (shared: Storage, now: number) => {
    for (let index = shared.length - 1; index >= 0; index--) {
        const key = shared.key(index);
        if (key?.startsWith(ALIVE_PREFIX) && now - Number(shared.getItem(key)) > SWEEP_AFTER_MS) {
            shared.removeItem(key);
        }
    }
};
