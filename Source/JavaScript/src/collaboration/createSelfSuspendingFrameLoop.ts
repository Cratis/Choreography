// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { SelfSuspendingFrameLoop } from './SelfSuspendingFrameLoop.js';

/**
 * Creates an animation-frame loop that suspends itself. It runs a frame only after being woken, and keeps
 * running frames for as long as the callback says there is more to do - so an idle surface costs nothing,
 * instead of paying for a permanent per-frame loop.
 * @param callback Runs once per frame with the frame's timestamp. Returns true when another frame is needed.
 * @returns The loop.
 */
export const createSelfSuspendingFrameLoop = (callback: (now: number) => boolean): SelfSuspendingFrameLoop => {
    let handle: number | undefined;
    let generation = 0;

    const request = (frame: (now: number) => void): number =>
        typeof globalThis.requestAnimationFrame === 'function'
            ? globalThis.requestAnimationFrame(frame)
            : (setTimeout(() => frame(performance.now()), 16) as unknown as number);

    const cancel = (scheduled: number) => {
        if (typeof globalThis.cancelAnimationFrame === 'function') {
            globalThis.cancelAnimationFrame(scheduled);
        } else {
            clearTimeout(scheduled);
        }
    };

    const schedule = () => {
        const current = generation;
        handle = request(now => {
            handle = undefined;
            const needsAnotherFrame = callback(now);

            // The callback may have stopped the loop, or woken it for the next frame itself; a stopped loop
            // must not come back on its own and a woken one must not be scheduled twice.
            if (needsAnotherFrame && current === generation && handle === undefined) {
                schedule();
            }
        });
    };

    return {
        wake() {
            if (handle === undefined) {
                schedule();
            }
        },
        stop() {
            generation++;
            if (handle !== undefined) {
                cancel(handle);
                handle = undefined;
            }
        }
    };
};
