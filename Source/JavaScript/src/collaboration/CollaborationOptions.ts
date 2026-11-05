// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CollaborationConnectionParameters } from './CollaborationConnectionParameters.js';

/**
 * Options for how the collaboration client reaches the backend.
 */
export interface CollaborationOptions {
    /** The path of the collaboration WebSocket endpoint on the page's own host. Defaults to `/ws/collaboration`. */
    path?: string;

    /**
     * Builds the complete WebSocket URL, for a backend on another host or with another query shape. When set,
     * {@link CollaborationOptions.path} is ignored.
     */
    buildUrl?: (connection: CollaborationConnectionParameters) => string;
}
