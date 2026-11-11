// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { contentPointOf } from '../contentPointOf.js';

describe('when mapping a screen point to content', () => {
    it('should undo the pan and the zoom', () => {
        expect(contentPointOf({ x: 250, y: 130 }, { zoom: 2, pan: { x: 50, y: 30 } })).toEqual({ x: 100, y: 50 });
    });

    it('should treat a zoom of zero as unscaled', () => {
        expect(contentPointOf({ x: 60, y: 40 }, { zoom: 0, pan: { x: 10, y: 10 } })).toEqual({ x: 50, y: 30 });
    });
});
