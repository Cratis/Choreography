// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import { orbitPlacement } from '../markerGeometry.js';
import type { OrbitPlacement } from '../OrbitPlacement.js';
import type { Point } from '../Point.js';

const HALF_WIDTH = 80;
const HALF_HEIGHT = 11;
const CORNER_RADIUS = 11;
const MARGIN = 6;

/** The direction the arrow is drawn pointing before any rotation is applied. */
const NATURAL_BEARING = -135;

/** The ends of the arrow's tail relative to its center, as the real cursor is proportioned. */
const TAIL: readonly Point[] = [{ x: 7.5, y: -1.25 }, { x: -1.25, y: 7.5 }];

/** The tail as it sits once the arrow has been turned to a bearing. */
const tailAimedAt = (bearing: number): Point[] => {
    const radians = ((bearing - NATURAL_BEARING) * Math.PI) / 180;
    const cosine = Math.cos(radians);
    const sine = Math.sin(radians);
    return TAIL.map(end => ({ x: end.x * cosine - end.y * sine, y: end.x * sine + end.y * cosine }));
};

const orbit = (bearing: number): OrbitPlacement =>
    orbitPlacement(bearing, HALF_WIDTH, HALF_HEIGHT, CORNER_RADIUS, MARGIN, tailAimedAt(bearing));

/** How far a point sits outside the pill - what has to stay constant all the way round. */
const clearanceOf = (x: number, y: number): number => {
    const outX = Math.max(Math.abs(x) - (HALF_WIDTH - CORNER_RADIUS), 0);
    const outY = Math.max(Math.abs(y) - (HALF_HEIGHT - CORNER_RADIUS), 0);
    return Math.hypot(outX, outY) - CORNER_RADIUS;
};

/** How close the arrow's tail comes to the pill at a bearing. */
const tailClearance = (bearing: number): number => {
    const placement = orbit(bearing);
    return Math.min(...tailAimedAt(bearing).map(end => clearanceOf(placement.x + end.x, placement.y + end.y)));
};

describe('when the arrow points right', () => {
    let result: OrbitPlacement;

    beforeEach(() => {
        result = orbit(0);
    });

    it('should sit beyond the cap', () => expect(result.x).toBeGreaterThan(HALF_WIDTH));
    it('should shape the tail to the cap it is on', () => expect(result.tailCurvature).toBeCloseTo(1, 2));
});

describe('when the arrow points down', () => {
    let result: OrbitPlacement;

    beforeEach(() => {
        result = orbit(90);
    });

    it('should sit below the straight run', () => expect(result.y).toBeGreaterThan(HALF_HEIGHT));
    it('should leave the tail flat against the straight run', () => expect(result.tailCurvature).toBe(0));
});

describe('when the arrow travels all the way around', () => {
    const bearings = Array.from({ length: 360 }, (_, index) => index);

    it('should hold the tail the same margin off the pill throughout', () => {
        for (const bearing of bearings) {
            expect(Math.abs(tailClearance(bearing) - MARGIN)).toBeLessThanOrEqual(0.01);
        }
    });

    it('should never let the tail touch the pill', () => {
        for (const bearing of bearings) {
            expect(tailClearance(bearing)).toBeGreaterThan(0);
        }
    });

    it('should never change the tail shape abruptly', () => {
        let previous = orbit(0).tailCurvature;
        for (const bearing of bearings) {
            const current = orbit(bearing).tailCurvature;
            expect(Math.abs(current - previous)).toBeLessThanOrEqual(0.5);
            previous = current;
        }
    });

    it('should morph the tail across many bearings rather than switching between two shapes', () => {
        const partly = bearings
            .map(bearing => orbit(bearing).tailCurvature)
            .filter(curvature => curvature > 0.01 && curvature < 0.99);

        // Four transitions - onto and off each cap - and the caps are a small share of a pill this wide,
        // so a handful of bearings each is what a genuine morph looks like here.
        expect(partly.length).toBeGreaterThanOrEqual(12);
    });
});

describe('when the pill is a bare corner', () => {
    let result: OrbitPlacement;

    beforeEach(() => {
        // The arrow resting in the pill's corner while the collaborator is on screen is this same
        // problem with a pill that is all cap, so it has to come out fully hollowed.
        result = orbitPlacement(
            NATURAL_BEARING,
            CORNER_RADIUS,
            CORNER_RADIUS,
            CORNER_RADIUS,
            MARGIN,
            tailAimedAt(NATURAL_BEARING));
    });

    it('should shape the tail to the corner', () => expect(result.tailCurvature).toBeCloseTo(1, 2));

    it('should hold the tail the margin off the corner', () => {
        const closest = Math.min(...TAIL.map(end => Math.hypot(result.x + end.x, result.y + end.y) - CORNER_RADIUS));
        expect(Math.abs(closest - MARGIN)).toBeLessThanOrEqual(0.01);
    });
});
