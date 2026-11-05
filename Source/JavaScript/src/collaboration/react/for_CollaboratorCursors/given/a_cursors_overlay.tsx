// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, render } from '@testing-library/react';
import { vi } from 'vitest';
import type { CanvasTransform } from '../../../CanvasTransform.js';
import { a_collaboration_room } from '../../../testing/a_collaboration_room.js';
import { CollaboratorCursors, type CollaboratorCursorsProperties } from '../../CollaboratorCursors.js';

/**
 * A cursors overlay in a fake room, with animation frames under the spec's control: frames are collected and
 * only run when the spec says so.
 */
export class a_cursors_overlay extends a_collaboration_room {
    readonly transform: { current: CanvasTransform } = { current: { zoom: 1, pan: { x: 0, y: 0 } } };
    readonly frames = new Map<number, FrameRequestCallback>();
    private _nextHandle = 1;

    constructor() {
        super();
        vi.stubGlobal('requestAnimationFrame', (callback: FrameRequestCallback) => {
            this.frames.set(this._nextHandle, callback);
            return this._nextHandle++;
        });
        vi.stubGlobal('cancelAnimationFrame', (handle: number) => this.frames.delete(handle));
    }

    /** Renders the overlay with the properties the spec cares about. */
    mount(properties: Partial<CollaboratorCursorsProperties> = {}) {
        return render(<CollaboratorCursors room='room' area='board' surfaceId='surface-1' transformRef={this.transform} {...properties} />);
    }

    /** Runs every frame that is scheduled right now, as the browser would at the given time. */
    runFrames(timestamp = performance.now() + 1_000): void {
        const scheduled = [...this.frames.values()];
        this.frames.clear();
        scheduled.forEach(frame => frame(timestamp));
    }

    dispose(): void {
        cleanup();
        super.dispose();
    }
}
