// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CanvasTransform } from './CanvasTransform.js';
import { contentPointOf } from './contentPointOf.js';
import type { Point } from './Point.js';
import type { PresenceSink } from './PresenceSink.js';

/**
 * Tells the others where this session's pointer is on a canvas, in the canvas' content space.
 * @param sink The collaboration client, when there is one; nothing is sent without it.
 * @param screen The pointer, relative to the canvas' screen area.
 * @param transform The canvas' current pan and zoom; nothing is sent before the canvas has one.
 * @param area The area of the application the canvas belongs to.
 * @param surfaceId The individual canvas within the area.
 */
export const reportCanvasPresence = (
    sink: PresenceSink | undefined,
    screen: Point,
    transform: CanvasTransform | null | undefined,
    area: string,
    surfaceId: string): void => {
    if (!sink || !transform) return;
    const content = contentPointOf(screen, transform);
    sink.sendPresence(content.x, content.y, area, surfaceId);
};
