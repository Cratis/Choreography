// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { positionAt } from '../cursorPath.js';
import type { CursorSample } from '../CursorSample.js';

const samples: CursorSample[] = [
    { x: 0, y: 0, time: 0 },
    { x: 10, y: 20, time: 100 },
    { x: 20, y: 40, time: 200 },
    { x: 30, y: 60, time: 300 }
];

describe('when interpolating samples', () => {
    describe('and there are none', () => {
        it('should have no position', () => expect(positionAt([], 50)).toBeUndefined());
    });

    describe('and the time is before the first sample', () => {
        it('should rest on the first sample', () => expect(positionAt(samples, -50)).toEqual(samples[0]));
    });

    describe('and the time is after the last sample', () => {
        it('should rest on the last sample', () => expect(positionAt(samples, 500)).toEqual(samples[3]));
    });

    describe('and the time falls exactly on a sample', () => {
        it('should be at that sample', () => {
            const position = positionAt(samples, 100)!;
            expect(position.x).toBeCloseTo(10, 6);
            expect(position.y).toBeCloseTo(20, 6);
        });
    });

    describe('and the samples lie on a straight line', () => {
        it('should glide along the line between two samples', () => {
            const position = positionAt(samples, 150)!;
            expect(position.x).toBeCloseTo(15, 6);
            expect(position.y).toBeCloseTo(30, 6);
        });
    });

    describe('and two samples share a time', () => {
        it('should not divide by zero', () => {
            const position = positionAt([{ x: 0, y: 0, time: 0 }, { x: 5, y: 5, time: 100 }, { x: 8, y: 8, time: 100 }, { x: 9, y: 9, time: 200 }], 100)!;
            expect(Number.isFinite(position.x)).toBe(true);
        });
    });
});
