// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * An animation-frame loop that stops asking for frames when it has nothing left to do.
 */
export interface SelfSuspendingFrameLoop {
    /** Makes sure a frame runs. Does nothing when a frame is already scheduled. */
    wake(): void;

    /** Cancels the scheduled frame. The loop can be woken again afterwards. */
    stop(): void;
}
