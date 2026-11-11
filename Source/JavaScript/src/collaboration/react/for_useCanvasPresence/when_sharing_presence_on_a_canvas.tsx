// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, cleanup, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { a_collaboration_room } from '../../testing/a_collaboration_room.js';
import { useCanvasPresence } from '../useCanvasPresence.js';
import { useCollaborationClient } from '../useCollaborationClient.js';

const surface = {
    getBoundingClientRect: () => ({ left: 10, top: 20, width: 400, height: 200 }) as DOMRect,
} as Element;

describe('when sharing presence on a canvas', () => {
    let context: a_collaboration_room;
    const transform = { zoom: 2, pan: { x: 0, y: 0 } };

    beforeEach(() => {
        vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'performance'] });
        vi.advanceTimersByTime(1000);
        context = new a_collaboration_room();
        transform.pan = { x: 0, y: 0 };
    });

    afterEach(() => {
        cleanup();
        context.dispose();
        vi.useRealTimers();
    });

    const presences = () => context.socket.sentMessages.filter(message => message.type === 'presence');
    const render = () => renderHook(() => useCanvasPresence({
        room: 'room', area: 'board', surfaceId: 'surface-1', transformRef: { current: transform }, surfaceRef: { current: surface },
    }));

    it('should announce the middle of the canvas as soon as it opens', () => {
        render();
        context.deliverRoster([]);
        vi.advanceTimersByTime(100);
        expect(presences().at(-1)).toMatchObject({ x: 100, y: 50, area: 'board', surfaceId: 'surface-1' });
    });

    it('should share where the pointer is over the content', () => {
        const { result } = render();
        context.deliverRoster([]);
        act(() => result.current.handlePointerMove({ clientX: 210, clientY: 120 }));
        vi.advanceTimersByTime(100);
        expect(presences().at(-1)).toMatchObject({ x: 100, y: 50 });
    });

    it('should follow the content under a pointer that stands still while the canvas pans', () => {
        const { result } = render();
        context.deliverRoster([]);
        act(() => result.current.handlePointerMove({ clientX: 210, clientY: 120 }));
        transform.pan = { x: 100, y: 0 };
        act(() => result.current.handleTransformChange());
        vi.advanceTimersByTime(100);
        expect(presences().at(-1)).toMatchObject({ x: 50, y: 50 });
    });

    it('should leave the area when the canvas closes while the room stays open', () => {
        renderHook(() => useCollaborationClient('room'));
        const { unmount } = render();
        context.deliverRoster([]);
        unmount();
        vi.advanceTimersByTime(100);
        expect(presences().at(-1)).toMatchObject({ area: '', surfaceId: '' });
    });
});
