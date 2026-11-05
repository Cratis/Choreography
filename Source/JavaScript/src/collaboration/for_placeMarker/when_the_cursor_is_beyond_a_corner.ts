// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import type { MarkerPlacement } from '../MarkerPlacement.js';
import { INSET_X, INSET_Y, VIEWPORT_HEIGHT, VIEWPORT_WIDTH, place } from './given/place.js';

describe('when the cursor is beyond the top left corner', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(-100, -160);
    });

    it('should pull the marker into the top left corner', () => expect(result.x).toBe(INSET_X));
    it('should pull the marker down to the top inset', () => expect(result.y).toBe(INSET_Y));
    it('should point the arrow up and to the left', () => expect(result.bearing).toBe(-135));
});

describe('when the cursor is beyond the top right corner', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(1100, -160);
    });

    it('should pull the marker into the top right corner', () => expect(result.x).toBe(VIEWPORT_WIDTH - INSET_X));
    it('should pull the marker down to the top inset', () => expect(result.y).toBe(INSET_Y));
    it('should point the arrow up and to the right', () => expect(result.bearing).toBe(-45));
});

describe('when the cursor is beyond the bottom right corner', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(1100, 760);
    });

    it('should pull the marker into the bottom right corner', () => expect(result.x).toBe(VIEWPORT_WIDTH - INSET_X));
    it('should pull the marker up to the bottom inset', () => expect(result.y).toBe(VIEWPORT_HEIGHT - INSET_Y));
    it('should point the arrow down and to the right', () => expect(result.bearing).toBe(45));
});

describe('when the cursor is beyond the bottom left corner', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(-100, 760);
    });

    it('should pull the marker into the bottom left corner', () => expect(result.x).toBe(INSET_X));
    it('should pull the marker up to the bottom inset', () => expect(result.y).toBe(VIEWPORT_HEIGHT - INSET_Y));
    it('should point the arrow down and to the left', () => expect(result.bearing).toBe(135));
});
