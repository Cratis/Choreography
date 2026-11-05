// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { arrowPath } from '../arrowPath.js';

/** The point of the arrow, which no tail shape is allowed to touch. */
const POINT = 'M2 2';

/** The radius of the arc drawn across the tail, or undefined when the tail was drawn flat. */
const tailRadiusOf = (path: string): number | undefined =>
    path.includes('A') ? Number(path.split('A')[1].trim().split(' ')[0]) : undefined;

describe('when the tail is against a straight run', () => {
    const result = arrowPath(Number.POSITIVE_INFINITY);

    it('should draw the tail flat', () => expect(result).not.toContain('A'));
    it('should keep the point', () => expect(result).toContain(POINT));
    it('should span straight between the barbs', () => expect(result).toContain('L14 7 L7 14'));
    it('should close the outline', () => expect(result).toContain('Z'));
});

describe('when the tail is around a cap', () => {
    const result = arrowPath(11);

    it('should draw the tail as an arc', () => expect(result).toContain('A'));
    it('should keep the point', () => expect(result).toContain(POINT));
    it('should curve on the radius it was given', () => expect(tailRadiusOf(result)).toBeCloseTo(11, 2));
    it('should still run from barb to barb', () => expect(result).toContain('L14 7 A'));
    it('should close the outline', () => expect(result).toContain('Z'));
    it('should hollow the tail rather than bulge it', () => expect(result).toContain('0 0 0'));
});

describe('when the tail is part way around', () => {
    it('should curve more tightly the further around it is', () =>
        expect(tailRadiusOf(arrowPath(11))!).toBeLessThan(tailRadiusOf(arrowPath(22))!));

    it('should never curve tighter than the tail is wide', () => {
        // The barbs are 7 units apart on each axis, so nothing shallower than that half-span can reach
        // across them - asking for less has to settle at the semicircle instead of failing to draw.
        const halfSpan = Math.hypot(7, 7) / 2;

        for (const asked of [0.5, 1, 2, 3, 4, 5]) {
            expect(tailRadiusOf(arrowPath(asked))!).toBeGreaterThanOrEqual(halfSpan - 0.01);
        }
    });
});

describe('when the tail is given no usable radius', () => {
    it('should draw it flat for zero', () => expect(tailRadiusOf(arrowPath(0))).toBeUndefined());
    it('should draw it flat for a negative radius', () => expect(tailRadiusOf(arrowPath(-4))).toBeUndefined());
    it('should draw it flat for a value that is not a number', () => expect(tailRadiusOf(arrowPath(Number.NaN))).toBeUndefined());
});
