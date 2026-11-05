// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The live state of one remote collaborator session, as delivered over the collaboration WebSocket.
 * Cursor coordinates are in surface content space - each client maps them through its own pan/zoom.
 */
export interface CollaboratorInfo {
    /** The session (browser tab) the collaborator is present with. */
    sessionId: string;

    /** The user the session belongs to. A user with several tabs open has several sessions. */
    userId: string;

    /** The name to show for the collaborator. */
    displayName: string;

    /** The collaborator's color as a hex value without the leading `#`. */
    color: string;

    /** The cursor's horizontal position in surface content coordinates. */
    x: number;

    /** The cursor's vertical position in surface content coordinates. */
    y: number;

    /** The area of the application the session is in, for example `board`. Empty when it is in none. */
    area: string;

    /** The surface within the area the session is looking at, for example one board. Empty when it is in none. */
    surfaceId: string;

    /** Whether the session has been idle. */
    isIdle: boolean;

    /** Whether the collaborator has an uploaded avatar; a host rendering avatars only asks for the image when there is one. */
    hasAvatar?: boolean;
}
