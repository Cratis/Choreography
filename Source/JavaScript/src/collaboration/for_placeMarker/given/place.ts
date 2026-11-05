// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { placeMarker } from '../../markerGeometry.js';
import type { MarkerPlacement } from '../../MarkerPlacement.js';

export const VIEWPORT_WIDTH = 1000;
export const VIEWPORT_HEIGHT = 600;
export const INSET_X = 100;
export const INSET_Y = 40;

export const place = (x: number, y: number): MarkerPlacement =>
    placeMarker(x, y, VIEWPORT_WIDTH, VIEWPORT_HEIGHT, INSET_X, INSET_Y);
