// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it, vi } from 'vitest';
import { reportCanvasPresence } from '../reportCanvasPresence.js';

describe('when reporting presence on a canvas', () => {
    const transform = { zoom: 2, pan: { x: 50, y: 30 } };

    it('should send the pointer in content coordinates', () => {
        const sink = { sendPresence: vi.fn() };
        reportCanvasPresence(sink, { x: 250, y: 130 }, transform, 'board', 'surface-1');
        expect(sink.sendPresence).toHaveBeenCalledWith(100, 50, 'board', 'surface-1');
    });

    it('should send nothing before the canvas has a transform', () => {
        const sink = { sendPresence: vi.fn() };
        reportCanvasPresence(sink, { x: 250, y: 130 }, null, 'board', 'surface-1');
        expect(sink.sendPresence).not.toHaveBeenCalled();
    });

    it('should do nothing without a client', () => {
        expect(() => reportCanvasPresence(undefined, { x: 1, y: 1 }, transform, 'board', 'surface-1')).not.toThrow();
    });
});
