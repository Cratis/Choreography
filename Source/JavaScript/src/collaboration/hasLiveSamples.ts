// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CursorSample } from './CursorSample.js';

/**
 * Whether any collaborator's sample buffer still has motion left to render at the given render time.
 * The follower evaluates each cursor's curve slightly in the past (the render delay), so a buffer whose
 * newest sample is at or before that time has been fully walked - the cursor rests on its last sample and
 * re-evaluating produces the same position every frame. Only a sample newer than the render time calls
 * for another animation frame.
 * @param buffers The sample buffers, one per collaborator.
 * @param renderTime The time the cursors are being rendered at.
 * @returns True when another frame is needed.
 */
export const hasLiveSamples = (buffers: Iterable<CursorSample[]>, renderTime: number): boolean => {
    for (const samples of buffers) {
        const newest = samples[samples.length - 1];
        if (newest && newest.time > renderTime) {
            return true;
        }
    }
    return false;
};
