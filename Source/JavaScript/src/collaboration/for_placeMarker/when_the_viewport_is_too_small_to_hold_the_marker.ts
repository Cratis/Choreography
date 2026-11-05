// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import { placeMarker } from '../markerGeometry.js';
import type { MarkerPlacement } from '../MarkerPlacement.js';

describe('when the viewport is narrower and shorter than the marker needs', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = placeMarker(-500, -500, 100, 50, 100, 40);
    });

    it('should report it as off screen', () => expect(result.isOffScreen).toBe(true));
    it('should center the marker horizontally rather than push it out of view', () => expect(result.x).toBe(50));
    it('should center the marker vertically rather than push it out of view', () => expect(result.y).toBe(25));
});

describe('when the viewport has not been measured yet', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = placeMarker(120, 80, 0, 0, 100, 40);
    });

    it('should report it as off screen', () => expect(result.isOffScreen).toBe(true));
    it('should not produce a position outside the viewport', () => expect(result.x).toBe(0));
    it('should not produce a vertical position outside the viewport', () => expect(result.y).toBe(0));
});
