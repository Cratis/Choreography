// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Announces that the canvas pan or zoom moved, so the cursors can be repositioned while no presence arrives.
 */
export interface CanvasTransformActivity {
    /**
     * Listens for transform changes.
     * @param listener Called when the transform may have changed.
     * @returns A function that stops listening.
     */
    subscribe(listener: () => void): () => void;
}
