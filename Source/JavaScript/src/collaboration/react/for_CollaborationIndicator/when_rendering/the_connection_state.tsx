// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { an_indicator } from '../given/an_indicator.js';

describe('when the connection is down for a long time', () => {
    let context: an_indicator;
    let container: HTMLElement;

    beforeEach(() => {
        vi.useFakeTimers();
        context = new an_indicator();
        container = context.mount().container;
        context.deliverRoster([]);
    });

    afterEach(() => {
        context.dispose();
        vi.useRealTimers();
    });

    describe('and it has only just dropped', () => {
        beforeEach(() => {
            act(() => {
                context.socket.onclose?.();
                vi.advanceTimersByTime(5_000);
            });
        });

        it('should stay quiet', () => {
            expect(container.querySelector('.collaboration-indicator--reconnecting')).toBeNull();
            expect(screen.queryByRole('status')).toBeNull();
        });
    });

    describe('and the reconnect threshold has passed', () => {
        beforeEach(() => {
            act(() => {
                context.socket.onclose?.();
                vi.advanceTimersByTime(30_000);
            });
        });

        it('should turn the indicator into its reconnecting state', () =>
            expect(container.querySelector('.collaboration-indicator--reconnecting')).not.toBeNull());
        it('should say why in the tooltip', () =>
            expect(screen.getByRole('button').getAttribute('title')).toBe('Reconnecting collaboration…'));
        it('should announce the status to assistive technology', () =>
            expect(screen.getByRole('status').textContent).toBe('Reconnecting collaboration…'));

        it('should say so in the open panel instead of listing people', () => {
            context.open();
            const panel = screen.getByRole('dialog');
            expect(panel.textContent).toContain('Reconnecting collaboration…');
            expect(panel.querySelector('.collaboration-indicator__list')).toBeNull();
        });

        it('should recover when the roster arrives again', () => {
            act(() => {
                vi.advanceTimersByTime(2_000);
            });
            context.deliverRoster([]);
            expect(container.querySelector('.collaboration-indicator--reconnecting')).toBeNull();
            expect(screen.queryByRole('status')).toBeNull();
        });
    });
});
