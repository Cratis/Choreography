// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi, type Mock } from 'vitest';
import { createSelfSuspendingFrameLoop } from '../createSelfSuspendingFrameLoop.js';

describe('when running frames', () => {
    let pending: Map<number, FrameRequestCallback>;
    let nextHandle: number;

    /** Runs the frames that are scheduled right now - not the ones they schedule in turn. */
    const runFrame = (timestamp = 16) => {
        const frames = [...pending.entries()];
        pending.clear();
        for (const [, frame] of frames) frame(timestamp);
    };

    /** Wakes a loop whose callback acts on the loop itself and asks for more, then runs one frame. */
    const runOneFrameWhere = (act: (loop: ReturnType<typeof createSelfSuspendingFrameLoop>) => void) => {
        const loop: ReturnType<typeof createSelfSuspendingFrameLoop> = createSelfSuspendingFrameLoop(() => {
            act(loop);
            return true;
        });
        loop.wake();
        runFrame();
    };

    beforeEach(() => {
        pending = new Map();
        nextHandle = 1;
        vi.stubGlobal('requestAnimationFrame', (callback: FrameRequestCallback) => {
            pending.set(nextHandle, callback);
            return nextHandle++;
        });
        vi.stubGlobal('cancelAnimationFrame', (handle: number) => pending.delete(handle));
    });

    afterEach(() => vi.unstubAllGlobals());

    describe('and the loop was never woken', () => {
        it('should not ask for a frame', () => {
            createSelfSuspendingFrameLoop(() => true);
            expect(pending.size).toBe(0);
        });
    });

    describe('and the loop is woken', () => {
        it('should ask for a frame and pass the callback the frame timestamp', () => {
            const callback = vi.fn(() => false);
            createSelfSuspendingFrameLoop(callback).wake();
            expect(pending.size).toBe(1);

            runFrame(123);
            expect(callback).toHaveBeenCalledWith(123);
        });

        it('should ask for one frame however many times it is woken', () => {
            const loop = createSelfSuspendingFrameLoop(() => false);
            loop.wake();
            loop.wake();
            loop.wake();
            expect(pending.size).toBe(1);
        });
    });

    describe('and the callback says there is more to do', () => {
        it('should ask for the next frame', () => {
            const loop = createSelfSuspendingFrameLoop(() => true);
            loop.wake();
            runFrame();
            expect(pending.size).toBe(1);
        });
    });

    describe('and the callback says it is done', () => {
        let callback: Mock<(now: number) => boolean>;
        let loop: ReturnType<typeof createSelfSuspendingFrameLoop>;

        beforeEach(() => {
            callback = vi.fn<(now: number) => boolean>(() => false);
            loop = createSelfSuspendingFrameLoop(callback);
            loop.wake();
            runFrame();
        });

        it('should suspend without asking for another frame', () => expect(pending.size).toBe(0));

        it('should run again when woken', () => {
            loop.wake();
            runFrame();
            expect(callback).toHaveBeenCalledTimes(2);
        });
    });

    describe('and the loop is stopped with a frame scheduled', () => {
        it('should cancel the frame', () => {
            const callback = vi.fn(() => true);
            const loop = createSelfSuspendingFrameLoop(callback);
            loop.wake();
            loop.stop();
            expect(pending.size).toBe(0);
            expect(callback).not.toHaveBeenCalled();
        });

        it('should run again when woken afterwards', () => {
            const callback = vi.fn(() => false);
            const loop = createSelfSuspendingFrameLoop(callback);
            loop.wake();
            loop.stop();
            loop.wake();
            runFrame();
            expect(callback).toHaveBeenCalledTimes(1);
        });
    });

    describe('and the callback stops the loop while asking for more', () => {
        it('should not come back on its own', () => {
            runOneFrameWhere(loop => loop.stop());
            expect(pending.size).toBe(0);
        });
    });

    describe('and the callback wakes the loop itself while asking for more', () => {
        it('should not schedule two frames', () => {
            runOneFrameWhere(loop => loop.wake());
            expect(pending.size).toBe(1);
        });
    });

    describe('and the environment has no animation frames', () => {
        it('should fall back to a timer', () => {
            vi.unstubAllGlobals();
            vi.stubGlobal('requestAnimationFrame', undefined);
            vi.stubGlobal('cancelAnimationFrame', undefined);
            vi.useFakeTimers();
            try {
                const callback = vi.fn(() => false);
                createSelfSuspendingFrameLoop(callback).wake();
                vi.advanceTimersByTime(20);
                expect(callback).toHaveBeenCalledTimes(1);
            } finally {
                vi.useRealTimers();
            }
        });
    });
});
