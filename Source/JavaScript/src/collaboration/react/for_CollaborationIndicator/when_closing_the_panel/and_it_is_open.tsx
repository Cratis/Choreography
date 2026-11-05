// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { fireEvent, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { an_indicator } from '../given/an_indicator.js';

describe('when the panel is open', () => {
    let context: an_indicator;

    beforeEach(() => {
        context = new an_indicator();
        context.mount();
        context.deliverRoster([]);
        context.open();
    });

    afterEach(() => context.dispose());

    it('should expose that it is expanded', () => expect(screen.getByRole('button').getAttribute('aria-expanded')).toBe('true'));

    describe('and the trigger is pressed again', () => {
        beforeEach(() => fireEvent.click(screen.getByRole('button')));

        it('should close', () => expect(screen.queryByRole('dialog')).toBeNull());
    });

    describe('and escape is pressed', () => {
        beforeEach(() => fireEvent.keyDown(document, { key: 'Escape' }));

        it('should close', () => expect(screen.queryByRole('dialog')).toBeNull());
    });

    describe('and another key is pressed', () => {
        beforeEach(() => fireEvent.keyDown(document, { key: 'a' }));

        it('should stay open', () => expect(screen.queryByRole('dialog')).not.toBeNull());
    });

    describe('and the page is pressed outside it', () => {
        beforeEach(() => fireEvent.pointerDown(document.body));

        it('should close', () => expect(screen.queryByRole('dialog')).toBeNull());
    });

    describe('and the panel itself is pressed', () => {
        beforeEach(() => fireEvent.pointerDown(screen.getByRole('dialog')));

        it('should stay open', () => expect(screen.queryByRole('dialog')).not.toBeNull());
    });
});
