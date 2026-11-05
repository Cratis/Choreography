// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CollaborationConnectionState } from '../CollaborationConnectionState.js';

/**
 * How the indicator's button looks and what its tooltip says.
 */
export interface CollaborationButton {
    /** The class names of the indicator's container. */
    className: string;

    /** The tooltip. */
    title: string;
}

/**
 * Works out the indicator button for a connection state. While reconnecting, the button itself turns red and
 * struck through, and its tooltip says why.
 * @param state The connection state.
 * @param tooltip The ordinary tooltip.
 * @param reconnectingLabel The tooltip while reconnecting.
 * @returns The class names and tooltip.
 */
export const collaborationButton = (state: CollaborationConnectionState, tooltip: string, reconnectingLabel: string): CollaborationButton =>
    state === CollaborationConnectionState.Reconnecting
        ? { className: 'collaboration-indicator collaboration-indicator--reconnecting', title: reconnectingLabel }
        : { className: 'collaboration-indicator', title: tooltip };
