// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { collaboratorInfoOf } from '../../../testing/collaboratorInfoOf.js';
import { ARROW_CLASS } from '../../CollaboratorCursor.js';
import { a_cursors_overlay } from '../given/a_cursors_overlay.js';

describe('when the canvas is panned while nobody moves', () => {
    let context: a_cursors_overlay;
    let listeners: Set<() => void>;
    let unsubscribed: number;
    let view: ReturnType<a_cursors_overlay['mount']>;

    beforeEach(() => {
        context = new a_cursors_overlay();
        listeners = new Set();
        unsubscribed = 0;
        view = context.mount({
            transformActivity: {
                subscribe: listener => {
                    listeners.add(listener);
                    return () => {
                        listeners.delete(listener);
                        unsubscribed++;
                    };
                }
            }
        });
        context.deliverRoster([collaboratorInfoOf({ sessionId: 'a', userId: 'ada', x: 10, y: 20 })]);

        // Walk the follower until it has caught up with the collaborator and suspended itself.
        for (let frame = 0; frame < 3; frame++) context.runFrames(performance.now() + 5_000);
    });

    afterEach(() => context.dispose());

    it('should listen for transform activity', () => expect(listeners.size).toBe(1));
    it('should have suspended the follower, asking for no more frames', () => expect(context.frames.size).toBe(0));

    describe('and the transform moves', () => {
        beforeEach(() => {
            context.transform.current = { zoom: 1, pan: { x: 100, y: 50 } };
            act(() => listeners.forEach(listener => listener()));
            context.runFrames(performance.now() + 5_000);
        });

        it('should wake the follower and reposition the arrow through the new transform', () => {
            const arrow = view.container.querySelector(`.${ARROW_CLASS}`) as HTMLElement;
            expect(arrow.style.left).toBe('110px');
            expect(arrow.style.top).toBe('70px');
        });

        it('should suspend again once repositioned', () => expect(context.frames.size).toBe(0));
    });

    describe('and the overlay is removed', () => {
        beforeEach(() => view.unmount());

        it('should stop listening for transform activity', () => expect(unsubscribed).toBeGreaterThan(0));
        it('should leave no listener behind', () => expect(listeners.size).toBe(0));
        it('should cancel the frame it had scheduled', () => expect(context.frames.size).toBe(0));
    });
});

describe('when no transform activity is given', () => {
    let context: a_cursors_overlay;

    beforeEach(() => {
        context = new a_cursors_overlay();
    });

    afterEach(() => context.dispose());

    it('should still follow presence', () => {
        const { container } = context.mount();
        context.deliverRoster([collaboratorInfoOf({ sessionId: 'a', userId: 'ada', x: 7, y: 9 })]);
        context.runFrames();
        expect((container.querySelector(`.${ARROW_CLASS}`) as HTMLElement).style.left).toBe('7px');
    });

    it('should not throw when the transform reference is empty', () => {
        const { container } = context.mount({ transformRef: { current: null } });
        context.deliverRoster([collaboratorInfoOf({ sessionId: 'a', userId: 'ada', x: 7, y: 9 })]);
        expect(() => context.runFrames()).not.toThrow();
        expect((container.querySelector(`.${ARROW_CLASS}`) as HTMLElement).style.left).toBe('7px');
    });
});

