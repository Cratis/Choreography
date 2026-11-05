// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { arrowPath } from './arrowPath.js';
import { orbitPlacement, placeMarker } from './markerGeometry.js';
import { ARROW_SIZE, ARROW_UNIT, PILL_CORNER_RADIUS, TAIL_CLEARANCE, TAIL_ENDS } from './cursorMetrics.js';
import type { CursorParts } from './CursorParts.js';
import type { Point } from './Point.js';

/** How far the arrow's box can reach from its own center once rotated: its half-diagonal. */
const ARROW_REACH = Math.round((ARROW_SIZE / 2) * Math.SQRT2);

/**
 * The direction the drawn arrow points with no rotation applied — up and to the left, as a mouse
 * pointer does. Rotations are expressed relative to it so a bearing maps straight onto a transform.
 */
const NATURAL_ARROW_BEARING = -135;

const DEGREES_PER_RADIAN = 180 / Math.PI;

/** Moves an element, skipping same-value writes so a settled cursor emits no mutation record. */
const moveTo = (element: HTMLElement, x: number, y: number) => {
    const left = `${x}px`;
    const top = `${y}px`;
    if (element.style.left !== left) element.style.left = left;
    if (element.style.top !== top) element.style.top = top;
};

/**
 * Aims the arrow at a bearing, or restores its natural pointer pose when given none. Whole degrees
 * are plenty and keep a moving off-screen collaborator from rewriting the transform every frame.
 * @param arrow The arrow element.
 * @param bearing The direction to aim at in degrees, or undefined for the natural pointer pose.
 */
const aimAt = (arrow: HTMLElement, bearing: number | undefined) => {
    const transform = bearing === undefined ? '' : `rotate(${Math.round(bearing - NATURAL_ARROW_BEARING)}deg)`;
    if (arrow.style.transform !== transform) arrow.style.transform = transform;
};

/**
 * The ends of the tail as they sit once the arrow has been aimed, so the placement measures its margin
 * from where they actually are rather than from where they are drawn.
 * @param bearing The direction the arrow is aimed at, in degrees.
 * @returns The tail's ends relative to the arrow's center, in pixels.
 */
const tailAimedAt = (bearing: number): Point[] => {
    const radians = ((bearing - NATURAL_ARROW_BEARING) / DEGREES_PER_RADIAN);
    const cosine = Math.cos(radians);
    const sine = Math.sin(radians);
    return TAIL_ENDS.map(end => ({ x: end.x * cosine - end.y * sine, y: end.x * sine + end.y * cosine }));
};

/**
 * Reshapes the arrow's tail to echo the pill where it currently rests. Quantized for the same reason the
 * rotation is: rewriting the outline is a change of what the layer paints rather than a move, so it is
 * worth doing only when the shape has visibly moved on.
 *
 * The tail is held a clearance off the pill, so it curves on the corner grown by that clearance rather
 * than on the corner itself — concentric with what it is wrapping, which is what keeps the gap even
 * across the whole tail instead of only at its middle.
 * @param parts The cursor's elements.
 * @param tailCurvature How far around a cap the tail is, from zero on a straight run to one on a cap.
 * @param cornerRadius The pill's effective corner radius, in pixels.
 */
const shapeTail = (parts: CursorParts, tailCurvature: number, cornerRadius: number) => {
    const quantized = Math.round(tailCurvature * 32) / 32;
    if (!parts.outline || quantized === parts.tailCurvature) return;

    parts.tailCurvature = quantized;
    const radius = quantized <= 0
        ? Number.POSITIVE_INFINITY
        : (cornerRadius + TAIL_CLEARANCE) / quantized / ARROW_UNIT;
    parts.outline.setAttribute('d', arrowPath(radius));
};

/**
 * Places one collaborator's cursor for the screen point their pointer is at. Inside the viewport this
 * is the cursor everyone knows — the arrow on the point, the pill resting in its tail. Outside it the
 * pill parks against the nearest edge and the arrow travels around the pill, aimed the way the local
 * user would have to pan to reach them, so a collaborator who has scrolled away is still on the surface.
 *
 * Written straight to the elements rather than through React: this runs on every animation frame, and
 * only the elements' position and rotation change, which a compositing host can follow by rect without repainting.
 * @param parts The cursor's elements; the pill's size is measured into them on first use.
 * @param x The collaborator's pointer x in viewport pixels.
 * @param y The collaborator's pointer y in viewport pixels.
 * @param viewportWidth The width of the visible viewport, in pixels.
 * @param viewportHeight The height of the visible viewport, in pixels.
 */
export function positionCursor(parts: CursorParts, x: number, y: number, viewportWidth: number, viewportHeight: number) {
    if (!parts.pillWidth) {
        parts.pillWidth = parts.pill.offsetWidth;
        parts.pillHeight = parts.pill.offsetHeight;
    }

    const halfWidth = parts.pillWidth / 2;
    const halfHeight = parts.pillHeight / 2;
    const cornerRadius = Math.min(PILL_CORNER_RADIUS, halfWidth, halfHeight);
    const placement = placeMarker(
        x,
        y,
        viewportWidth,
        viewportHeight,
        halfWidth + TAIL_CLEARANCE + ARROW_REACH,
        halfHeight + TAIL_CLEARANCE + ARROW_REACH);

    // A surface that has not been laid out yet (or is hidden) has no edges to park anyone against —
    // draw every cursor where it actually is until there is a viewport to clamp into.
    const isMeasured = viewportWidth > 0 && viewportHeight > 0;
    if (!isMeasured || !placement.isOffScreen) {
        // On screen the pill hangs off the arrow rather than the other way round, but what the tail rests
        // against is only the pill's near corner — which is a circle of the corner radius, so it is the
        // orbit again with a pill that is all cap and no straight run. Solving it the same way is what
        // makes the margin identical whether the collaborator is on screen or off it.
        const corner = orbitPlacement(
            NATURAL_ARROW_BEARING,
            cornerRadius,
            cornerRadius,
            cornerRadius,
            TAIL_CLEARANCE,
            tailAimedAt(NATURAL_ARROW_BEARING));

        moveTo(parts.arrow, x, y);
        moveTo(
            parts.pill,
            x + ARROW_SIZE / 2 - corner.x - cornerRadius,
            y + ARROW_SIZE / 2 - corner.y - cornerRadius);
        aimAt(parts.arrow, undefined);
        shapeTail(parts, corner.tailCurvature, cornerRadius);
        return;
    }

    const orbit = orbitPlacement(
        placement.bearing,
        halfWidth,
        halfHeight,
        PILL_CORNER_RADIUS,
        TAIL_CLEARANCE,
        tailAimedAt(placement.bearing));
    moveTo(parts.pill, placement.x - halfWidth, placement.y - halfHeight);
    moveTo(parts.arrow, placement.x + orbit.x - ARROW_SIZE / 2, placement.y + orbit.y - ARROW_SIZE / 2);
    aimAt(parts.arrow, placement.bearing);
    shapeTail(parts, orbit.tailCurvature, cornerRadius);
}
