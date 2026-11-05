// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import type { MarkerPlacement } from '../MarkerPlacement.js';
import { INSET_X, INSET_Y, VIEWPORT_HEIGHT, VIEWPORT_WIDTH, place } from './given/place.js';

describe('when the cursor is beyond the left edge', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(-200, 300);
    });

    it('should report it as off screen', () => expect(result.isOffScreen).toBe(true));
    it('should pull the marker to the left inset', () => expect(result.x).toBe(INSET_X));
    it('should keep the marker level with the collaborator', () => expect(result.y).toBe(300));
    it('should point the arrow left', () => expect(result.bearing).toBe(180));
});

describe('when the cursor is beyond the right edge', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(1500, 300);
    });

    it('should report it as off screen', () => expect(result.isOffScreen).toBe(true));
    it('should pull the marker to the right inset', () => expect(result.x).toBe(VIEWPORT_WIDTH - INSET_X));
    it('should keep the marker level with the collaborator', () => expect(result.y).toBe(300));
    it('should point the arrow right', () => expect(result.bearing).toBe(0));
});

describe('when the cursor is above the top edge', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(500, -100);
    });

    it('should report it as off screen', () => expect(result.isOffScreen).toBe(true));
    it('should pull the marker to the top inset', () => expect(result.y).toBe(INSET_Y));
    it('should keep the marker in line with the collaborator', () => expect(result.x).toBe(500));
    it('should point the arrow up', () => expect(result.bearing).toBe(-90));
});

describe('when the cursor is below the bottom edge', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(500, 900);
    });

    it('should report it as off screen', () => expect(result.isOffScreen).toBe(true));
    it('should pull the marker to the bottom inset', () => expect(result.y).toBe(VIEWPORT_HEIGHT - INSET_Y));
    it('should keep the marker in line with the collaborator', () => expect(result.x).toBe(500));
    it('should point the arrow down', () => expect(result.bearing).toBe(90));
});

describe('when the cursor is only just past the right edge', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(VIEWPORT_WIDTH + 1, 300);
    });

    it('should report it as off screen', () => expect(result.isOffScreen).toBe(true));
    it('should pull the marker all the way in to the inset', () => expect(result.x).toBe(VIEWPORT_WIDTH - INSET_X));
    it('should point the arrow right', () => expect(result.bearing).toBe(0));
});
