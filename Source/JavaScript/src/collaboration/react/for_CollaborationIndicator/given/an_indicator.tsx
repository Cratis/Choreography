// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { a_collaboration_room } from '../../../testing/a_collaboration_room.js';
import { CollaborationIndicator, type CollaborationIndicatorProperties } from '../../CollaborationIndicator.js';

/** A collaboration indicator in a fake room. */
export class an_indicator extends a_collaboration_room {
    mount(properties: Partial<CollaborationIndicatorProperties> = {}) {
        return render(<CollaborationIndicator room='room' {...properties} />);
    }

    /** Opens the panel through the default trigger. */
    open(): void {
        fireEvent.click(screen.getByRole('button'));
    }

    dispose(): void {
        cleanup();
        super.dispose();
    }
}
