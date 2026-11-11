// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CanvasTransform } from './CanvasTransform.js';
import type { Point } from './Point.js';

/**
 * Maps a point on a canvas' screen area to the canvas' content space, which is what collaborators exchange so that
 * each of them can place a cursor through their own pan and zoom: `content = (screen - pan) / zoom`.
 * @param screen The point, relative to the canvas' screen area.
 * @param transform The canvas' current pan and zoom.
 * @returns The point in content coordinates.
 */
export const contentPointOf = (screen: Point, transform: CanvasTransform): Point => {
    const zoom = transform.zoom || 1;
    return { x: (screen.x - transform.pan.x) / zoom, y: (screen.y - transform.pan.y) / zoom };
};
