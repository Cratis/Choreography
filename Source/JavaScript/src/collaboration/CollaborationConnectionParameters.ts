// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * What identifies a collaboration connection to the backend.
 */
export interface CollaborationConnectionParameters {
    /** The room to join. Everyone in the same room sees each other. */
    room: string;

    /** The identifier of this session (browser tab). */
    sessionId: string;

    /** This session's color as a hex value without the leading `#`. */
    color: string;
}
