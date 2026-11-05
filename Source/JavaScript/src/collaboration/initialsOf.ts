// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The initials to show in place of an avatar: the first letters of the first and last word of the name.
 * @param name The person's display name.
 * @returns One or two upper-case letters, or `?` when the name has none.
 */
export const initialsOf = (name: string): string => {
    const words = name.trim().split(/\s+/).filter(word => word.length > 0);
    if (words.length === 0) return '?';
    const first = [...words[0]][0];
    const last = words.length > 1 ? [...words[words.length - 1]][0] : '';
    return `${first}${last}`.toUpperCase();
};
