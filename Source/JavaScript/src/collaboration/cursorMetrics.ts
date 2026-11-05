// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ARROW_VIEWBOX, TAIL_ENDS_IN_BOX } from './arrowPath.js';
import type { Point } from './Point.js';

/**
 * The pill's corner radius in pixels, matching `border-radius` in the stylesheet. Kept here as well
 * because the arrow's tail is shaped to echo it, and the geometry needs the number rather than the rule.
 */
export const PILL_CORNER_RADIUS = 11;

/** The rendered size of the arrow, in pixels — it is drawn in a square box. */
export const ARROW_SIZE = 20;

/** How many pixels one unit of the arrow's own coordinate box is drawn at. */
export const ARROW_UNIT = ARROW_SIZE / ARROW_VIEWBOX;

/**
 * The clear space kept between the arrow's tail and the pill, in pixels. One number for both poses: the
 * arrow rests the same distance off the pill whether the collaborator is on screen or being pointed at
 * from the edge, which is what stops the gap from reading as an accident of where they happen to be.
 */
export const TAIL_CLEARANCE = 3;

/** The ends of the arrow's tail, relative to the center of its box, in pixels. */
export const TAIL_ENDS: readonly Point[] = TAIL_ENDS_IN_BOX.map(end => ({ x: end.x * ARROW_UNIT, y: end.y * ARROW_UNIT }));

/**
 * The radius the tail curves on while the arrow rests against a corner, in the arrow's box units. The
 * arrow sits a clearance off the corner, so the tail is concentric with the corner grown by that
 * clearance rather than with the corner itself — which is what keeps the gap even across the whole tail
 * rather than only at its middle.
 */
export const CORNER_TAIL_RADIUS = (PILL_CORNER_RADIUS + TAIL_CLEARANCE) / ARROW_UNIT;
