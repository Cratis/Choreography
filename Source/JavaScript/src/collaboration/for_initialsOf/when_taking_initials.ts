// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { describe, expect, it } from 'vitest';
import { initialsOf } from '../initialsOf.js';

describe('when taking initials', () => {
    it('should use the first and last word of a full name', () => expect(initialsOf('Ada Augusta King')).toBe('AK'));
    it('should use one letter for a single word', () => expect(initialsOf('einar')).toBe('E'));
    it('should upper-case the letters', () => expect(initialsOf('ada lovelace')).toBe('AL'));
    it('should ignore surrounding whitespace', () => expect(initialsOf('   Ada   Lovelace  ')).toBe('AL'));
    it('should fall back to a question mark for an empty name', () => expect(initialsOf('   ')).toBe('?'));
    it('should not split a character outside the basic plane', () => expect(initialsOf('\u{1F600} Smith')).toBe('\u{1F600}S'));
});
