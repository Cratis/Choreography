// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CollaborationConnectionState } from '../../../CollaborationConnectionState.js';
import { collaboratorInfoOf } from '../../../testing/collaboratorInfoOf.js';
import type { CollaborationIndicatorTriggerState } from '../../CollaborationIndicatorTriggerState.js';
import { an_indicator } from '../given/an_indicator.js';

describe('when customizing the indicator', () => {
    let context: an_indicator;

    beforeEach(() => {
        context = new an_indicator();
    });

    afterEach(() => context.dispose());

    describe('and the labels are replaced', () => {
        it('should use the host\'s strings and keep English for the rest', () => {
            const { container } = context.mount({
                labels: { title: 'Aktive samarbeidspartnere', onlyYou: 'Bare deg', collaborators: '{count} samarbeidspartnere' }
            });
            context.deliverRoster([]);
            context.open();

            expect(screen.getByRole('button').getAttribute('title')).toBe('Aktive samarbeidspartnere');
            expect(screen.getByRole('dialog').textContent).toContain('Bare deg');
            expect(container.querySelector('.collaboration-indicator__header')?.textContent).toBe('Aktive samarbeidspartnere');
        });

        it('should fill the count into a replaced label', () => {
            const { container } = context.mount({ labels: { collaborators: '{count} samarbeidspartnere', tabs: '{count} faner' } });
            context.deliverRoster([
                collaboratorInfoOf({ sessionId: 'a', userId: 'ada', displayName: 'Ada' }),
                collaboratorInfoOf({ sessionId: 'b', userId: 'ada', displayName: 'Ada' })
            ]);
            context.open();

            expect(container.querySelector('.collaboration-indicator__count-badge')?.getAttribute('aria-label')).toBe('1 samarbeidspartnere');
            expect(screen.getByRole('dialog').textContent).toContain('2 faner');
        });
    });

    describe('and an avatar renderer is given', () => {
        it('should draw the avatars of the panel with it, at the size of the panel', () => {
            const renderAvatar = vi.fn(subject => <img alt={subject.displayName} src={`${subject.userId}.png`} />);
            context.mount({ renderAvatar });
            context.deliverRoster([collaboratorInfoOf({ sessionId: 'a', userId: 'ada', displayName: 'Ada', hasAvatar: true })]);
            context.open();

            expect(screen.getByAltText('Ada').getAttribute('src')).toBe('ada.png');
            expect(renderAvatar).toHaveBeenCalledWith(expect.objectContaining({ userId: 'ada', hasAvatar: true, tabs: 1 }), 28);
        });
    });

    describe('and the host draws the trigger', () => {
        let states: CollaborationIndicatorTriggerState[];
        let container: HTMLElement;

        beforeEach(() => {
            states = [];
            container = context.mount({
                renderTrigger: state => {
                    states.push(state);
                    return <button type='button' data-testid='host-trigger' onClick={state.toggle}>People {state.count}</button>;
                }
            }).container;
            context.deliverRoster([collaboratorInfoOf({ sessionId: 'a', userId: 'ada' })]);
        });

        it('should use the host\'s button instead of its own', () => {
            expect(screen.getByTestId('host-trigger')).not.toBeNull();
            expect(container.querySelector('.collaboration-indicator__button')).toBeNull();
        });

        it('should tell the trigger how many people are here', () => expect(states[states.length - 1].count).toBe(1));
        it('should tell the trigger the connection state', () =>
            expect(states[states.length - 1].connectionState).toBe(CollaborationConnectionState.Connected));
        it('should tell the trigger the tooltip', () => expect(states[states.length - 1].title).toBe('Active collaborators'));
        it('should still show the badge', () => expect(container.querySelector('.collaboration-indicator__count-badge')).not.toBeNull());

        it('should open the panel when the trigger toggles it', () => {
            fireEvent.click(screen.getByTestId('host-trigger'));
            expect(screen.getByRole('dialog')).not.toBeNull();
            expect(states[states.length - 1].isOpen).toBe(true);
        });
    });

    describe('and the panel is aligned to the start', () => {
        it('should say so on the panel', () => {
            context.mount({ panelAlignment: 'start' });
            context.open();
            expect(screen.getByRole('dialog').className).toContain('collaboration-indicator__panel--start');
        });
    });

    describe('and an extra class name is given', () => {
        it('should apply it to the container', () => {
            const { container } = context.mount({ className: 'in-the-toolbar' });
            expect(container.querySelector('.collaboration-indicator.in-the-toolbar')).not.toBeNull();
        });
    });
});
