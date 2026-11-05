// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { collaboratorInfoOf } from '../../../testing/collaboratorInfoOf.js';
import { ARROW_CLASS, PILL_CLASS } from '../../CollaboratorCursor.js';
import { a_cursors_overlay } from '../given/a_cursors_overlay.js';

describe('when rendering the cursors of a surface', () => {
    let context: a_cursors_overlay;

    beforeEach(() => {
        context = new a_cursors_overlay();
    });

    afterEach(() => context.dispose());

    describe('and others are on this surface, another surface and in another area', () => {
        let container: HTMLElement;

        beforeEach(() => {
            container = context.mount().container;
            context.deliverRoster([
                collaboratorInfoOf({ sessionId: 'a', userId: 'ada', displayName: 'Ada', surfaceId: 'surface-1' }),
                collaboratorInfoOf({ sessionId: 'b', userId: 'bob', displayName: 'Bob', surfaceId: 'surface-2' }),
                collaboratorInfoOf({ sessionId: 'c', userId: 'cyd', displayName: 'Cyd', area: 'notes' }),
                collaboratorInfoOf({ sessionId: 'd', userId: 'me', displayName: 'Me' })
            ]);
        });

        it('should draw only the collaborators looking at this surface', () =>
            expect([...container.querySelectorAll('.collaborator-cursor__name')].map(name => name.textContent)).toEqual(['Ada']));

        it('should fill its container and let pointer events through, hidden from assistive technology', () => {
            const overlay = container.querySelector('.collaborator-cursors');
            expect(overlay?.getAttribute('aria-hidden')).toBe('true');
        });
    });

    describe('and a collaborator arrives at a position', () => {
        let container: HTMLElement;

        beforeEach(() => {
            context.transform.current = { zoom: 2, pan: { x: 5, y: 5 } };
            container = context.mount().container;
            context.deliverRoster([collaboratorInfoOf({ sessionId: 'a', userId: 'ada', x: 10, y: 20 })]);
            context.runFrames();
        });

        it('should place the arrow through the live pan and zoom', () => {
            const arrow = container.querySelector(`.${ARROW_CLASS}`) as HTMLElement;
            expect(arrow.style.left).toBe('25px');
            expect(arrow.style.top).toBe('45px');
        });

        it('should place the pill beside the arrow', () => {
            const pill = container.querySelector(`.${PILL_CLASS}`) as HTMLElement;
            expect(pill.style.left).not.toBe('');
        });
    });

    describe('and the collaborators are laid out', () => {
        it('should apply the extra class name to the overlay', () => {
            const { container } = context.mount({ className: 'over-the-canvas' });
            expect(container.querySelector('.collaborator-cursors.over-the-canvas')).not.toBeNull();
        });
    });

    describe('and layer attributes and an avatar renderer are given', () => {
        it('should pass both through to every cursor', () => {
            const { container } = context.mount({
                layerProps: { 'data-layer': 'over' },
                renderAvatar: subject => <b data-avatar={subject.displayName}>{subject.displayName[0]}</b>
            });
            context.deliverRoster([
                collaboratorInfoOf({ sessionId: 'a', userId: 'ada', displayName: 'Ada' }),
                collaboratorInfoOf({ sessionId: 'b', userId: 'bob', displayName: 'Bob' })
            ]);

            expect(container.querySelectorAll('[data-layer="over"]')).toHaveLength(4);
            expect([...container.querySelectorAll('[data-avatar]')].map(avatar => avatar.getAttribute('data-avatar'))).toEqual(['Ada', 'Bob']);
        });
    });
});
