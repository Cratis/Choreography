// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CollaborationConnectionState } from '../CollaborationConnectionState.js';

/**
 * What a host needs to draw its own trigger button for the {@link CollaborationIndicator}.
 */
export interface CollaborationIndicatorTriggerState {
    /** Opens the panel when it is closed and closes it when it is open. Call it from the button's click. */
    toggle(): void;

    /** Whether the panel is open. */
    isOpen: boolean;

    /** The tooltip to give the button: the title, or the reconnecting message while reconnecting. */
    title: string;

    /** The state of the connection. */
    connectionState: CollaborationConnectionState;

    /** How many other people are here. */
    count: number;
}
