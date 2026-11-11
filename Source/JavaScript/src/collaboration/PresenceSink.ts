// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Where presence is sent: a `CollaborationClient`, or anything that can stand in for one. */
export interface PresenceSink {
    /**
     * Sends this session's position on a surface.
     * @param x The horizontal position in the surface's content space.
     * @param y The vertical position in the surface's content space.
     * @param area The area of the application the surface belongs to.
     * @param surfaceId The individual surface within the area.
     */
    sendPresence(x: number, y: number, area: string, surfaceId: string): void;
}
