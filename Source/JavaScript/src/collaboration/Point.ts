// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * A point in two dimensions. Which space it belongs to - surface content or the local viewport - is
 * whatever the producing function documents; a collaborator's position travels through both.
 */
export interface Point {
    /** The horizontal coordinate. */
    x: number;

    /** The vertical coordinate. */
    y: number;
}
