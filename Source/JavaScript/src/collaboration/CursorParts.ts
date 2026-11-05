// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The elements one collaborator's cursor is made of. The arrow and the pill are siblings rather than
 * one element, so a host can give each its own layer (see the `layerProps` of the cursors component).
 */
export interface CursorParts {
    /** The pointer arrow. */
    arrow: HTMLElement;

    /** The pill carrying the collaborator's avatar and name. */
    pill: HTMLElement;

    /** The arrow's outline, reshaped as its tail travels between the pill's straight runs and its caps. */
    outline: SVGPathElement | null;

    /**
     * The tail curvature the outline was last drawn at. Redrawing a path is a change of what the layer
     * paints rather than movement, so it is only rewritten when the shape has actually moved on.
     */
    tailCurvature: number;

    /** The pill's measured width in pixels; 0 until it has been laid out. */
    pillWidth: number;

    /** The pill's measured height in pixels; 0 until it has been laid out. */
    pillHeight: number;
}
