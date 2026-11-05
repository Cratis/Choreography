// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Where the arrow sits while orbiting the pill, and the shape its tail takes there.
 */
export interface OrbitPlacement {
    /** The arrow's offset from the pill's center along x, in pixels. */
    x: number;

    /** The arrow's offset from the pill's center along y, in pixels. */
    y: number;

    /**
     * How much of the pill's corner the arrow's tail is sitting against: zero along the pill's straight
     * runs, and one once fully around a cap. The tail is shaped from this rather than being handed a
     * radius, because the radius it curves on depends on how far off the pill the tail is held.
     */
    tailCurvature: number;
}
