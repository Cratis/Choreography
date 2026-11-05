// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { arrowPath } from '../arrowPath.js';
import { CORNER_TAIL_RADIUS } from '../cursorMetrics.js';
import type { CollaboratorInfo } from '../CollaboratorInfo.js';
import { CollaboratorAvatar } from './CollaboratorAvatar.js';
import type { RenderCollaboratorAvatar } from './RenderCollaboratorAvatar.js';

/** Marks the arrow within a cursor, so the follower can find it without a fragile child lookup. */
export const ARROW_CLASS = 'collaborator-cursor__arrow';

/** Marks the avatar-and-name pill within a cursor, so the follower can find it. */
export const PILL_CLASS = 'collaborator-cursor__pill';

/** Marks the arrow's outline, so the follower can reshape its tail as it travels around the pill. */
export const OUTLINE_CLASS = 'collaborator-cursor__outline';

/** The size in pixels of the avatar in the pill. */
export const CURSOR_AVATAR_SIZE = 18;

/**
 * Properties for {@link CollaboratorCursor}.
 */
export interface CollaboratorCursorProperties {
    /** The collaborator this cursor belongs to. */
    collaborator: CollaboratorInfo;

    /** Receives the cursor's root element; the cursors follower positions its parts every frame. */
    nodeRef: (node: HTMLDivElement | null) => void;

    /** Attributes put on the arrow and on the pill elements, for example to mark them as layers for a compositing host. */
    layerProps?: Record<string, string>;

    /** Draws the avatar in the pill. Defaults to the collaborator's initials on their color. */
    renderAvatar?: RenderCollaboratorAvatar;
}

/**
 * One remote collaborator's cursor: the pointer arrow, and a pill carrying the collaborator's avatar and
 * name. While the collaborator is on screen the arrow is an ordinary mouse pointer with the pill nestled in
 * its tail; once they leave the visible area the pill parks at the nearest edge and the arrow travels around
 * it, aimed the way the local user would have to pan. Both are positioned directly by the interpolating
 * follower in {@link CollaboratorCursors}, never through React.
 *
 * The arrow and the pill are siblings rather than one element with a moving child, so each can be its own
 * layer in a compositing host: a rotating child would otherwise repaint the cursor on every frame.
 * @param properties The collaborator and how to draw them.
 * @returns The cursor.
 */
export const CollaboratorCursor = ({ collaborator, nodeRef, layerProps, renderAvatar }: CollaboratorCursorProperties) => {
    const color = `#${collaborator.color}`;
    const idle = collaborator.isIdle ? ' collaborator-cursor--idle' : '';

    return (
        <div ref={nodeRef} className='collaborator-cursor' aria-hidden='true'>
            <div className={`${ARROW_CLASS}${idle}`} {...layerProps}>
                <svg viewBox='0 0 16 16' xmlns='http://www.w3.org/2000/svg'>
                    <path
                        className={OUTLINE_CLASS}
                        d={arrowPath(CORNER_TAIL_RADIUS)}
                        fill={color}
                        stroke='#fff'
                        strokeWidth='1'
                    />
                </svg>
            </div>
            <div className={`${PILL_CLASS}${idle}`} style={{ background: color }} {...layerProps}>
                <span className='collaborator-cursor__avatar'>
                    {renderAvatar
                        ? renderAvatar(collaborator, CURSOR_AVATAR_SIZE)
                        : <CollaboratorAvatar subject={collaborator} size={CURSOR_AVATAR_SIZE} />}
                </span>
                <span className='collaborator-cursor__name'>{collaborator.displayName}</span>
            </div>
        </div>
    );
};
