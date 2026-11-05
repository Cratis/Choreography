// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Where a collaborator's marker is drawn in the local viewport, and which way its arrow points.
 */
export interface MarkerPlacement {
    /** The marker x in viewport pixels - the cursor's own x while on screen, pulled to the edge otherwise. */
    x: number;

    /** The marker y in viewport pixels - the cursor's own y while on screen, pulled to the edge otherwise. */
    y: number;

    /** Whether the collaborator's cursor is outside the locally visible part of the surface. */
    isOffScreen: boolean;

    /**
     * The bearing from the marker toward the collaborator, in degrees, with 0 pointing right (+x) and 90
     * pointing down (+y) as screen coordinates do. This is the direction the local user has to pan to
     * reach the collaborator. Always 0 while on screen, where the arrow keeps its natural cursor pose.
     */
    bearing: number;
}
