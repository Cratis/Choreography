// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { collaboratorInfoOf } from '../../../testing/collaboratorInfoOf.js';
import { an_indicator } from '../given/an_indicator.js';

describe('when rendering the people in the room', () => {
    let context: an_indicator;
    let container: HTMLElement;

    beforeEach(() => {
        context = new an_indicator();
    });

    afterEach(() => context.dispose());

    describe('and nobody else is here', () => {
        beforeEach(() => {
            container = context.mount().container;
            context.deliverRoster([]);
        });

        it('should show no badge', () => expect(container.querySelector('.collaboration-indicator__count-badge')).toBeNull());
        it('should keep the panel closed', () => expect(screen.queryByRole('dialog')).toBeNull());

        it('should say you are alone once opened', () => {
            context.open();
            expect(screen.getByRole('dialog').textContent).toContain('Only you');
        });
    });

    describe('and a person has two tabs open beside another person', () => {
        beforeEach(() => {
            container = context.mount({ areaLabels: { board: 'Event Model' } }).container;
            context.deliverRoster([
                collaboratorInfoOf({ sessionId: 'ada-1', userId: 'ada', displayName: 'Ada Lovelace', area: 'board' }),
                collaboratorInfoOf({ sessionId: 'ada-2', userId: 'ada', displayName: 'Ada Lovelace', area: 'board' }),
                collaboratorInfoOf({ sessionId: 'bob-1', userId: 'bob', displayName: 'Bob', area: 'notes', isIdle: true }),
                collaboratorInfoOf({ sessionId: 'me-2', userId: 'me', displayName: 'Me', area: 'board' })
            ]);
        });

        it('should count people, not tabs, in the badge', () =>
            expect(container.querySelector('.collaboration-indicator__count-badge')?.textContent).toBe('2'));
        it('should name the badge for assistive technology', () =>
            expect(container.querySelector('.collaboration-indicator__count-badge')?.getAttribute('aria-label')).toBe('2 collaborators'));

        it('should list each person once', () => {
            context.open();
            const names = [...container.querySelectorAll('.collaboration-indicator__item-name')].map(name => name.textContent);
            expect(names).toHaveLength(2);
            expect(names[0]).toContain('Ada Lovelace');
            expect(names[1]).toContain('Bob');
        });

        it('should show the tab count of a person with several tabs', () => {
            context.open();
            expect(container.querySelectorAll('.collaboration-indicator__item')[0].textContent).toContain('2 tabs');
        });

        it('should show the named area where there is a label, and the raw area where there is not', () => {
            context.open();
            const areas = [...container.querySelectorAll('.collaboration-indicator__item-area')].map(area => area.textContent);
            expect(areas).toEqual(['Event Model', 'notes']);
        });

        it('should mark a person who is idle', () => {
            context.open();
            const bob = container.querySelectorAll('.collaboration-indicator__item')[1];
            expect(bob.className).toContain('collaboration-indicator__item--idle');
            expect(bob.textContent).toContain('(idle)');
        });

        it('should draw initials on the person\'s color in place of an avatar', () => {
            context.open();
            expect(container.querySelector('.collaboration-indicator__avatar')?.textContent).toBe('AL');
        });

        it('should mention the user\'s own other tab', () => {
            context.open();
            expect(screen.getByRole('dialog').textContent).toContain('You are also open in 1 other tab(s)');
        });
    });

    describe('and the connection has not been confirmed', () => {
        it('should not list anyone before the roster arrives', () => {
            container = context.mount().container;
            context.open();
            expect(container.querySelector('.collaboration-indicator__list')).toBeNull();
        });
    });
});
