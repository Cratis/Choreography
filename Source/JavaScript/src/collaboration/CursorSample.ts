// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Point } from './Point.js';

/**
 * One remote cursor position in surface content space, stamped with the local time it arrived. A short
 * buffer of these is what the follower walks through to turn discrete presence samples into motion.
 */
export interface CursorSample extends Point {
    /** The local `performance.now()` reading at which the sample arrived. */
    time: number;
}
