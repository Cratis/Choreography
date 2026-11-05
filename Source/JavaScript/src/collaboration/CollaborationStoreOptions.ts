// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { CollaborationOptions } from './CollaborationOptions.js';

/**
 * Options for acquiring the shared collaboration client. They take effect when the acquisition creates the
 * client; acquiring the client that is already connected to the same room does not change it.
 */
export interface CollaborationStoreOptions extends CollaborationOptions {
    /** The color for this session as a hex value without the leading `#`. Defaults to one remembered per browser. */
    color?: string;
}
