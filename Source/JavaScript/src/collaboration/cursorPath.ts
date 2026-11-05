// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CursorSample } from './CursorSample.js';
import type { Point } from './Point.js';

/**
 * Catmull-Rom interpolation between `second` and `third`, with `first` and `fourth` shaping the tangents.
 * @param first The sample before the segment.
 * @param second The sample the segment starts at.
 * @param third The sample the segment ends at.
 * @param fourth The sample after the segment.
 * @param position How far along the segment, from 0 to 1.
 * @returns The interpolated value.
 */
const catmullRom = (first: number, second: number, third: number, fourth: number, position: number): number => {
    const squared = position * position;
    const cubed = squared * position;
    return 0.5 * ((2 * second) +
        (-first + third) * position +
        (2 * first - 5 * second + 4 * third - fourth) * squared +
        (-first + 3 * second - 3 * third + fourth) * cubed);
};

/**
 * The interpolated surface content position at a point in time, following the sample path through a
 * Catmull-Rom curve. Evaluating slightly in the past is what turns the ~30ms presence samples into one
 * continuous curved motion rather than a series of jumps.
 * @param samples The recent samples, in arrival order.
 * @param time The local time to evaluate at.
 * @returns The position, or undefined when there is nothing to evaluate.
 */
export function positionAt(samples: CursorSample[], time: number): Point | undefined {
    if (!samples.length) return undefined;
    if (time <= samples[0].time) return samples[0];
    const last = samples[samples.length - 1];
    if (time >= last.time) return last;

    let index = samples.length - 2;
    while (index > 0 && samples[index].time > time) index--;
    const start = samples[index];
    const end = samples[index + 1];
    const before = samples[index - 1] ?? start;
    const after = samples[index + 2] ?? end;
    const span = end.time - start.time;
    const progress = span > 0 ? (time - start.time) / span : 1;
    return {
        x: catmullRom(before.x, start.x, end.x, after.x, progress),
        y: catmullRom(before.y, start.y, end.y, after.y, progress),
    };
}
