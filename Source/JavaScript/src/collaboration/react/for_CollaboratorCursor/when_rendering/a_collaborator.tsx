// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, render } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { collaboratorInfoOf } from '../../../testing/collaboratorInfoOf.js';
import { ARROW_CLASS, CURSOR_AVATAR_SIZE, CollaboratorCursor, PILL_CLASS } from '../../CollaboratorCursor.js';

describe('when rendering a collaborator\'s cursor', () => {
    afterEach(cleanup);

    const draw = (properties: Partial<Parameters<typeof CollaboratorCursor>[0]> = {}) => {
        const { container } = render(
            <CollaboratorCursor
                collaborator={collaboratorInfoOf({ displayName: 'Ada Lovelace', color: '2196F3' })}
                nodeRef={() => undefined}
                {...properties} />);
        return container;
    };

    describe('and nothing is customized', () => {
        const container = () => draw();

        it('should be hidden from assistive technology', () =>
            expect(container().querySelector('.collaborator-cursor')?.getAttribute('aria-hidden')).toBe('true'));
        it('should show the name', () => expect(container().querySelector('.collaborator-cursor__name')?.textContent).toBe('Ada Lovelace'));
        it('should color the pill by the collaborator', () =>
            expect((container().querySelector(`.${PILL_CLASS}`) as HTMLElement).style.background).toBe('rgb(33, 150, 243)'));
        it('should color the arrow by the collaborator', () =>
            expect(container().querySelector(`.${ARROW_CLASS} path`)?.getAttribute('fill')).toBe('#2196F3'));
        it('should draw the initials in place of an avatar', () =>
            expect(container().querySelector('.collaborator-cursor__avatar')?.textContent).toBe('AL'));
        it('should not be dimmed', () => expect(container().querySelector('.collaborator-cursor--idle')).toBeNull());
    });

    describe('and the collaborator is idle', () => {
        it('should dim both the arrow and the pill', () => {
            const container = draw({ collaborator: collaboratorInfoOf({ isIdle: true }) });
            expect(container.querySelectorAll('.collaborator-cursor--idle')).toHaveLength(2);
        });
    });

    describe('and layer attributes are given', () => {
        it('should put them on the arrow and on the pill, and nowhere else', () => {
            const container = draw({ layerProps: { 'data-layer': 'over' } });
            const marked = [...container.querySelectorAll('[data-layer="over"]')];
            expect(marked).toHaveLength(2);
            expect(marked.map(element => element.className.split(' ')[0]).sort()).toEqual([ARROW_CLASS, PILL_CLASS].sort());
        });
    });

    describe('and an avatar renderer is given', () => {
        it('should draw the avatar it returns, at the size of the pill', () => {
            const renderAvatar = vi.fn(() => <img alt='portrait' src='portrait.png' />);
            const container = draw({ renderAvatar });
            expect(container.querySelector('.collaborator-cursor__avatar img')).not.toBeNull();
            expect(renderAvatar).toHaveBeenCalledWith(expect.objectContaining({ displayName: 'Ada Lovelace', color: '2196F3' }), CURSOR_AVATAR_SIZE);
        });

        it('should not draw the initials', () => {
            const container = draw({ renderAvatar: () => <img alt='portrait' src='portrait.png' /> });
            expect(container.querySelector('.collaboration-avatar')).toBeNull();
        });
    });
});
