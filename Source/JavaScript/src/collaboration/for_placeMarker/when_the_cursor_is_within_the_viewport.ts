// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import type { MarkerPlacement } from '../MarkerPlacement.js';
import { VIEWPORT_HEIGHT, VIEWPORT_WIDTH, place } from './given/place.js';

describe('when the cursor is well inside the viewport', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(500, 300);
    });

    it('should report it as on screen', () => expect(result.isOffScreen).toBe(false));
    it('should keep the exact x', () => expect(result.x).toBe(500));
    it('should keep the exact y', () => expect(result.y).toBe(300));
    it('should leave the arrow in its natural cursor pose', () => expect(result.bearing).toBe(0));
});

describe('when the cursor sits on the top left corner of the viewport', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(0, 0);
    });

    it('should report it as on screen', () => expect(result.isOffScreen).toBe(false));
    it('should keep the exact x', () => expect(result.x).toBe(0));
    it('should keep the exact y', () => expect(result.y).toBe(0));
});

describe('when the cursor sits on the bottom right corner of the viewport', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(VIEWPORT_WIDTH, VIEWPORT_HEIGHT);
    });

    it('should report it as on screen', () => expect(result.isOffScreen).toBe(false));
    it('should keep the exact x', () => expect(result.x).toBe(VIEWPORT_WIDTH));
    it('should keep the exact y', () => expect(result.y).toBe(VIEWPORT_HEIGHT));
});

describe('when the cursor is inside the viewport but within the marker inset', () => {
    let result: MarkerPlacement;

    beforeEach(() => {
        result = place(4, 3);
    });

    it('should still report it as on screen', () => expect(result.isOffScreen).toBe(false));
    it('should not pull the cursor away from where the collaborator is', () => expect(result.x).toBe(4));
    it('should not pull the cursor down from where the collaborator is', () => expect(result.y).toBe(3));
});
