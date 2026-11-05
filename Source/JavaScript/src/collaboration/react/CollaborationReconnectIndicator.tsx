// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { CollaborationConnectionState } from '../CollaborationConnectionState.js';

/**
 * Properties for {@link CollaborationReconnectIndicator}.
 */
export interface CollaborationReconnectIndicatorProperties {
    /** The connection state. */
    state: CollaborationConnectionState;

    /** The message to announce while reconnecting. */
    label: string;
}

/**
 * Announces, to assistive technology, that collaboration is reconnecting. Nothing is shown beside the icon -
 * the indicator button itself turns red - so the message is visually hidden.
 * @param properties The state and the message.
 * @returns The status element while reconnecting, otherwise nothing.
 */
export const CollaborationReconnectIndicator = ({ state, label }: CollaborationReconnectIndicatorProperties) =>
    state === CollaborationConnectionState.Reconnecting ? (
        <span className='collaboration-indicator__reconnecting' role='status'>{label}</span>
    ) : null;
