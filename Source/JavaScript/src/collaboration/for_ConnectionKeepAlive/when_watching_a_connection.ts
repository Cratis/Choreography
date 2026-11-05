// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ConnectionKeepAlive } from '../ConnectionKeepAlive.js';

describe('when watching a connection', () => {
    let onStale: ReturnType<typeof vi.fn>;
    let keepAlive: ConnectionKeepAlive;

    beforeEach(() => {
        vi.useFakeTimers();
        onStale = vi.fn();
        keepAlive = new ConnectionKeepAlive(1_000, onStale);
    });

    afterEach(() => {
        keepAlive.stop();
        vi.useRealTimers();
    });

    describe('and it was never started', () => {
        beforeEach(() => vi.advanceTimersByTime(60_000));

        it('should not report it stale', () => expect(onStale).not.toHaveBeenCalled());
        it('should not be running', () => expect(keepAlive.isRunning).toBe(false));
    });

    describe('and activity keeps arriving', () => {
        beforeEach(() => {
            keepAlive.start();
            for (let second = 0; second < 10; second++) {
                vi.advanceTimersByTime(1_000);
                keepAlive.recordActivity();
            }
        });

        it('should not report it stale', () => expect(onStale).not.toHaveBeenCalled());
        it('should still be running', () => expect(keepAlive.isRunning).toBe(true));
    });

    describe('and nothing is heard for less than twice the interval', () => {
        beforeEach(() => {
            keepAlive.start();
            vi.advanceTimersByTime(1_999);
        });

        it('should not report it stale', () => expect(onStale).not.toHaveBeenCalled());
    });

    describe('and nothing is heard for twice the interval', () => {
        beforeEach(() => {
            keepAlive.start();
            vi.advanceTimersByTime(2_000);
        });

        it('should report it stale once', () => expect(onStale).toHaveBeenCalledTimes(1));
        it('should stop watching', () => expect(keepAlive.isRunning).toBe(false));

        it('should not report it again while the silence continues', () => {
            vi.advanceTimersByTime(30_000);
            expect(onStale).toHaveBeenCalledTimes(1);
        });
    });

    describe('and a different stale threshold is given', () => {
        beforeEach(() => {
            keepAlive.stop();
            keepAlive = new ConnectionKeepAlive(1_000, onStale, 5_000);
            keepAlive.start();
            vi.advanceTimersByTime(4_000);
        });

        it('should wait for that threshold', () => expect(onStale).not.toHaveBeenCalled());
        it('should report it stale once reached', () => {
            vi.advanceTimersByTime(1_000);
            expect(onStale).toHaveBeenCalledTimes(1);
        });
    });

    describe('and it is stopped before it goes stale', () => {
        beforeEach(() => {
            keepAlive.start();
            vi.advanceTimersByTime(1_000);
            keepAlive.stop();
            vi.advanceTimersByTime(60_000);
        });

        it('should not report it stale', () => expect(onStale).not.toHaveBeenCalled());
    });

    describe('and it is started again after going stale', () => {
        beforeEach(() => {
            keepAlive.start();
            vi.advanceTimersByTime(2_000);
            keepAlive.start();
            vi.advanceTimersByTime(1_000);
        });

        it('should count the silence from the new start', () => expect(onStale).toHaveBeenCalledTimes(1));
    });
});
