// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import { hasLiveSamples } from '../hasLiveSamples.js';
import type { CursorSample } from '../CursorSample.js';

const sample = (time: number): CursorSample => ({ x: 0, y: 0, time });

describe('when no collaborator has a buffer', () => {
    it('should report nothing live', () => expect(hasLiveSamples([], 1_000)).toBe(false));
});

describe('when a buffer is empty', () => {
    it('should report nothing live', () => expect(hasLiveSamples([[]], 1_000)).toBe(false));
});

describe('when every buffer ends at or before the render time', () => {
    let result: boolean;

    beforeEach(() => {
        result = hasLiveSamples([[sample(400), sample(800)], [sample(1_000)]], 1_000);
    });

    it('should report nothing live', () => expect(result).toBe(false));
});

describe('when one buffer has a sample newer than the render time', () => {
    let result: boolean;

    beforeEach(() => {
        result = hasLiveSamples([[sample(400), sample(800)], [sample(900), sample(1_200)]], 1_000);
    });

    it('should report live motion', () => expect(result).toBe(true));
});
