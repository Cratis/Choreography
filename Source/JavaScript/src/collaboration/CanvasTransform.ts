// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Point } from './Point.js';

/**
 * The pan and zoom of the canvas the cursors are drawn over, used to map surface content coordinates to
 * the screen: `screen = content * zoom + pan`.
 */
export interface CanvasTransform {
    /** The zoom factor; 1 is unscaled. */
    zoom: number;

    /** The pan offset in screen pixels. */
    pan: Point;
}
