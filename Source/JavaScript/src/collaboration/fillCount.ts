// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Fills the `{count}` placeholder of a label.
 * @param template The label, for example `{count} tabs`.
 * @param count The number to put in.
 * @returns The label with the number in place.
 */
export const fillCount = (template: string, count: number): string => template.replaceAll('{count}', count.toString());
