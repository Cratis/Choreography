// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Point } from './Point.js';

/** The extent of the arrow's own square coordinate box, along each axis. */
export const ARROW_VIEWBOX = 16;

/** The point that does the pointing, and the two barbs the tail spans between. */
const POINT: Point = { x: 2, y: 2 };
const LEADING_BARB: Point = { x: 14, y: 7 };
const TRAILING_BARB: Point = { x: 7, y: 14 };

/** The center of the box, which is what the follower positions and rotates the arrow about. */
const CENTER: Point = { x: ARROW_VIEWBOX / 2, y: ARROW_VIEWBOX / 2 };

const format = (value: number): string => Number(value.toFixed(2)).toString();

/**
 * The ends of the tail, relative to the center of the box, in box units. The tail is what has to clear
 * the pill, and — being straight or hollowed, never bowed toward the pill — its ends are what come
 * closest to it, so these are the points placement keeps its margin from.
 */
export const TAIL_ENDS_IN_BOX: readonly Point[] = [
    { x: LEADING_BARB.x - CENTER.x, y: LEADING_BARB.y - CENTER.y },
    { x: TRAILING_BARB.x - CENTER.x, y: TRAILING_BARB.y - CENTER.y }];

/** Half the span between the barbs — the tightest arc that can still reach across the tail. */
const TAIL_HALF_SPAN = Math.hypot(TRAILING_BARB.x - LEADING_BARB.x, TRAILING_BARB.y - LEADING_BARB.y) / 2;

/**
 * The arrow's outline, with its tail curved on the given radius.
 *
 * The tail is the edge the pill sits against, so shaping it to the pill's own outline is what makes the
 * arrow read as resting there rather than being parked nearby: straight where the pill runs straight,
 * and hollowed out around a cap so it nests into the curve instead of cutting across it. The point is
 * never touched — a blunt point stops reading as an arrow, and it is the end nobody is looking at.
 * @param tailRadius The radius to curve the tail on, in the arrow's box units. Anything that is not a
 * finite positive number — `Infinity` included — draws the tail flat, which is what a straight run asks
 * for and what a straight edge is: an arc of unbounded radius.
 * @returns The SVG path data for the arrow.
 */
export function arrowPath(tailRadius: number): string {
    const toLeadingBarb = `M${format(POINT.x)} ${format(POINT.y)} L${format(LEADING_BARB.x)} ${format(LEADING_BARB.y)} `;
    const closeAtTrailingBarb = `${format(TRAILING_BARB.x)} ${format(TRAILING_BARB.y)} Z`;

    if (!Number.isFinite(tailRadius) || tailRadius <= 0) {
        return `${toLeadingBarb}L${closeAtTrailingBarb}`;
    }

    // An arc tighter than the tail is wide cannot reach across it. The semicircle standing on the barbs
    // is as deep as the tail can go, and is what a pill with a cap smaller than the arrow's tail asks for.
    const radius = Math.max(tailRadius, TAIL_HALF_SPAN);

    // Sweep 0 puts the arc's center on the far side of the tail from the point, so the tail is hollowed
    // toward the point — wrapping the cap it sits on — rather than bulging out into it.
    return `${toLeadingBarb}A${format(radius)} ${format(radius)} 0 0 0 ${closeAtTrailingBarb}`;
}
