// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** The colors a session is given from, as hex values without the leading `#`. */
export const AVATAR_COLORS: readonly string[] = [
    '4CAF50', '2196F3', 'FF5722', '9C27B0',
    'FF9800', '00BCD4', 'E91E63', '795548'
];

/** The storage key the browser remembers its color under. */
export const AVATAR_COLOR_KEY = 'collaboration:avatarColor';

/**
 * Gets the color this browser has been given, choosing and remembering one the first time. A remembered
 * value with a leading `#` is normalized to the form without it.
 * @param storage The storage that is shared by every tab of the browser.
 * @param random Gives a number from 0 up to but not including 1; replaceable for specs.
 * @returns A hex color without the leading `#`.
 */
export const getOrCreateAvatarColor = (storage: Storage, random: () => number = Math.random): string => {
    const stored = storage.getItem(AVATAR_COLOR_KEY);
    if (stored) {
        const normalized = stored.replace(/^#/, '');
        if (normalized !== stored) storage.setItem(AVATAR_COLOR_KEY, normalized);
        return normalized;
    }

    const color = AVATAR_COLORS[Math.floor(random() * AVATAR_COLORS.length)];
    storage.setItem(AVATAR_COLOR_KEY, color);
    return color;
};
