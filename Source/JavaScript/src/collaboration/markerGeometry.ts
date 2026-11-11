// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { MarkerPlacement } from './MarkerPlacement.js';
import type { OrbitPlacement } from './OrbitPlacement.js';
import type { Point } from './Point.js';

const DEGREES_PER_RADIAN = 180 / Math.PI;

/**
 * Keeps a value inside a range, centering it when the range has collapsed — a viewport too small to
 * hold the marker away from both edges has no valid position, and the middle is the least wrong one.
 * @param value The value to constrain.
 * @param minimum The lowest allowed value.
 * @param maximum The highest allowed value.
 * @returns The constrained value.
 */
const constrain = (value: number, minimum: number, maximum: number): number =>
    minimum > maximum ? (minimum + maximum) / 2 : Math.min(Math.max(value, minimum), maximum);

/** Whether a point lies within a viewport of the given size. */
const isWithinViewport = (x: number, y: number, viewportWidth: number, viewportHeight: number): boolean =>
    x >= 0 && y >= 0 && x <= viewportWidth && y <= viewportHeight;

/**
 * Places a collaborator's marker for a cursor sitting at a point in the local viewport. A cursor inside
 * the viewport keeps its exact position, so it reads as an ordinary mouse pointer where the collaborator
 * actually is. A cursor outside it is pulled to the nearest edge — far enough in that the whole marker
 * stays visible — and the bearing then says which way the local user has to pan to reach them.
 * @param x The collaborator's cursor x, in viewport pixels.
 * @param y The collaborator's cursor y, in viewport pixels.
 * @param viewportWidth The width of the visible viewport, in pixels.
 * @param viewportHeight The height of the visible viewport, in pixels.
 * @param insetX How far from the left and right edges the marker has to stay, in pixels.
 * @param insetY How far from the top and bottom edges the marker has to stay, in pixels.
 * @returns Where to draw the marker and which way its arrow points.
 */
export function placeMarker(
    x: number,
    y: number,
    viewportWidth: number,
    viewportHeight: number,
    insetX: number,
    insetY: number): MarkerPlacement {
    if (isWithinViewport(x, y, viewportWidth, viewportHeight)) {
        return { x, y, isOffScreen: false, bearing: 0 };
    }

    const markerX = constrain(x, insetX, viewportWidth - insetX);
    const markerY = constrain(y, insetY, viewportHeight - insetY);
    return {
        x: markerX,
        y: markerY,
        isOffScreen: true,
        bearing: Math.atan2(y - markerY, x - markerX) * DEGREES_PER_RADIAN,
    };
}

/** Smooth 0..1 ramp, so the tail changes shape continuously rather than snapping at a tangent. */
const smoothstep = (value: number): number => value * value * (3 - 2 * value);

/** Constrains to 0..1. */
const saturate = (value: number): number => Math.min(Math.max(value, 0), 1);

/**
 * How far past a straight run's end the contact has travelled, as 0..1. An axis with no straight run to
 * speak of - a pill is exactly as round as it is tall, leaving none vertically - is always fully past it,
 * which is what makes the tail round on the caps rather than only at their widest point. The rest ramp
 * across the corner's own radius, so the change reads as the tail rolling around the cap.
 * @param contact How far the contact point is from the center along the axis, in pixels.
 * @param coreExtent Half the straight run's length along that axis, in pixels.
 * @param cornerRadius The pill's corner radius, in pixels.
 * @returns 0 while on the straight run, ramping to 1 once fully around the corner.
 */
const pastStraightRun = (contact: number, coreExtent: number, cornerRadius: number): number =>
    coreExtent <= 0 ? 1 : smoothstep(saturate((contact - coreExtent) / cornerRadius));

/** How far a point sits outside the rectangle at the pill's core; zero anywhere within it. */
const distanceToCore = (x: number, y: number, coreHalfWidth: number, coreHalfHeight: number): number =>
    Math.hypot(Math.max(Math.abs(x) - coreHalfWidth, 0), Math.max(Math.abs(y) - coreHalfHeight, 0));

/** How many halvings the distance is cornered by — well past the point where a pixel could tell. */
const SOLVER_STEPS = 40;

/** What the orbit is solved against: the direction walked out along, the pill's core rectangle and the tail's ends. */
interface OrbitGeometry {
    readonly towardX: number;
    readonly towardY: number;
    readonly coreHalfWidth: number;
    readonly coreHalfHeight: number;
    readonly tail: readonly Point[];
}

/** How far an end of the tail sits from the core once the arrow has walked a distance out along the bearing. */
const reachOf = (geometry: OrbitGeometry, distance: number, point: Point): number =>
    distanceToCore(
        geometry.towardX * distance + point.x,
        geometry.towardY * distance + point.y,
        geometry.coreHalfWidth,
        geometry.coreHalfHeight);

// The tail is straight or hollowed, never bowed toward the pill, so whichever of its ends comes
// closest to the core is what the whole tail's clearance is.
const closestReach = (geometry: OrbitGeometry, distance: number): number =>
    geometry.tail.reduce((closest, point) => {
        const reach = reachOf(geometry, distance, point);
        return reach < closest ? reach : closest;
    }, Number.POSITIVE_INFINITY);

// Walking out along the bearing only ever carries the tail further from the core, so the distance
// that lands it exactly on the margin can be cornered by halving. The closed form would have to put
// a rotated pair of points against a rounded rectangle - a page of algebra for the same number.
const solveDistance = (geometry: OrbitGeometry, required: number): number => {
    const spread = geometry.tail.reduce((widest, point) => Math.max(widest, Math.hypot(point.x, point.y)), 0);
    let low = 0;
    let high = Math.hypot(geometry.coreHalfWidth, geometry.coreHalfHeight) + required + spread;
    for (let step = 0; step < SOLVER_STEPS; step++) {
        const middle = (low + high) / 2;
        if (closestReach(geometry, middle) < required) low = middle; else high = middle;
    }
    return high;
};

// Which end is doing the touching is what decides the tail's shape, not where the arrow's center
// happens to be: the contact is the end that comes closest to the core, taken as offsets from its center.
const contactAt = (geometry: OrbitGeometry, distance: number): Point => {
    let contact: Point = { x: 0, y: 0 };
    let closest = Number.POSITIVE_INFINITY;
    for (const point of geometry.tail) {
        const reach = reachOf(geometry, distance, point);
        if (reach >= closest) continue;
        closest = reach;
        contact = {
            x: Math.abs(geometry.towardX * distance + point.x),
            y: Math.abs(geometry.towardY * distance + point.y),
        };
    }
    return contact;
};

/**
 * Where the arrow sits while orbiting the pill, and what shape its tail should take there.
 *
 * What has to stay a constant margin off the pill is the arrow's *tail*, not the arrow's center — and
 * those are not the same problem, because the arrow is turned to face the collaborator rather than to
 * face away from the pill. Around a cap the two directions differ, so a tail held out by its center digs
 * one end into the pill and lifts the other clear of it. Placing it by the ends themselves is what makes
 * the margin the same all the way round.
 *
 * The pill is a rounded rectangle — the set of points within its corner radius of a smaller rectangle —
 * so holding the tail a margin off the pill is the same as holding it radius-plus-margin off that
 * rectangle, which is a distance anything can be measured against without special-casing the caps.
 * @param bearing The direction in degrees, with 0 pointing right (+x) and 90 pointing down (+y).
 * @param halfWidth Half the pill's width, in pixels.
 * @param halfHeight Half the pill's height, in pixels.
 * @param cornerRadius The pill's corner radius, in pixels.
 * @param margin How much clear space to keep between the pill and the tail, in pixels.
 * @param tail The ends of the arrow's tail relative to its center, already turned to the bearing, in pixels.
 * @returns The arrow center's offset from the pill's center, and how far around a cap the tail is there.
 */
export function orbitPlacement(
    bearing: number,
    halfWidth: number,
    halfHeight: number,
    cornerRadius: number,
    margin: number,
    tail: readonly Point[]): OrbitPlacement {
    const radius = Math.min(cornerRadius, halfWidth, halfHeight);

    // The rectangle the pill is grown from. A name pill is as round as it is tall, which collapses this
    // to a horizontal line - its ends being the centers of the two semicircular caps.
    const coreHalfWidth = Math.max(halfWidth - radius, 0);
    const coreHalfHeight = Math.max(halfHeight - radius, 0);

    const radians = bearing / DEGREES_PER_RADIAN;
    const geometry: OrbitGeometry = {
        towardX: Math.cos(radians),
        towardY: Math.sin(radians),
        coreHalfWidth,
        coreHalfHeight,
        tail,
    };
    const distance = solveDistance(geometry, radius + margin);

    // The tail mirrors the pill where it meets it: flat along the straight runs, and hollowed to the
    // cap's own curve once fully onto an arc. Blending across the tangent rather than switching keeps
    // the arrow from visibly changing shape in one frame as it travels.
    const contact = contactAt(geometry, distance);

    return {
        x: geometry.towardX * distance,
        y: geometry.towardY * distance,
        tailCurvature:
            pastStraightRun(contact.x, coreHalfWidth, radius)
            * pastStraightRun(contact.y, coreHalfHeight, radius),
    };
}
