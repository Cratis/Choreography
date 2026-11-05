// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { beforeEach, describe, expect, it } from 'vitest';
import { AVATAR_COLORS, AVATAR_COLOR_KEY, getOrCreateAvatarColor } from '../avatarColor.js';
import { MemoryStorage } from '../MemoryStorage.js';

describe('when choosing a color', () => {
    let storage: MemoryStorage;

    beforeEach(() => {
        storage = new MemoryStorage();
    });

    describe('and none is remembered', () => {
        it('should pick one of the colors by the random number', () => {
            expect(getOrCreateAvatarColor(storage, () => 0)).toBe(AVATAR_COLORS[0]);
            storage.clear();
            expect(getOrCreateAvatarColor(storage, () => 0.999)).toBe(AVATAR_COLORS[AVATAR_COLORS.length - 1]);
        });

        it('should remember it', () => {
            const color = getOrCreateAvatarColor(storage, () => 0.5);
            expect(storage.getItem(AVATAR_COLOR_KEY)).toBe(color);
        });

        it('should give the same color the next time whatever the random number', () => {
            const first = getOrCreateAvatarColor(storage, () => 0);
            expect(getOrCreateAvatarColor(storage, () => 0.9)).toBe(first);
        });
    });

    describe('and a color with a leading hash is remembered', () => {
        beforeEach(() => storage.setItem(AVATAR_COLOR_KEY, '#ABCDEF'));

        it('should return it without the hash', () => expect(getOrCreateAvatarColor(storage)).toBe('ABCDEF'));

        it('should rewrite what is remembered without the hash', () => {
            getOrCreateAvatarColor(storage);
            expect(storage.getItem(AVATAR_COLOR_KEY)).toBe('ABCDEF');
        });
    });
});
