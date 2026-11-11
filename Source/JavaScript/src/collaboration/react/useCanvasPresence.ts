// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useCallback, useEffect, useRef } from 'react';
import type { CanvasTransform } from '../CanvasTransform.js';
import type { CollaborationStoreOptions } from '../CollaborationStoreOptions.js';
import type { Point } from '../Point.js';
import { reportCanvasPresence } from '../reportCanvasPresence.js';
import { useCollaborationClient } from './useCollaborationClient.js';

/** Where a canvas is, for {@link useCanvasPresence}. */
export interface CanvasPresenceInput {
    /** The collaboration room, or undefined when it is not known yet. */
    room: string | undefined;

    /** The area of the application the canvas belongs to. */
    area: string;

    /** The individual canvas within the area. */
    surfaceId: string;

    /** The canvas' live pan and zoom - the same transform its `CollaboratorCursors` draw through. */
    transformRef: { readonly current: CanvasTransform | null | undefined };

    /** The element the canvas fills, which the pointer is measured against. */
    surfaceRef: { readonly current: Element | null | undefined };

    /** How to reach the backend; applied when this canvas is the first to connect to the room. */
    options?: CollaborationStoreOptions;
}

/** The handlers {@link useCanvasPresence} returns to wire onto a canvas. */
export interface CanvasPresenceHandlers {
    /** Wire onto the canvas' pointer move. */
    handlePointerMove: (event: { clientX: number; clientY: number }) => void;

    /** Call whenever the canvas pans or zooms, so the pointer's content position follows the content under it. */
    handleTransformChange: () => void;
}

/**
 * Shares this session's position on a canvas with the others in the room: where it is the moment the canvas opens,
 * where the pointer goes, where the pointer is over the content after a pan or zoom, and that it left when the canvas
 * closes. The room's client is held for as long as the canvas is mounted, so a canvas never depends on something else
 * having connected first.
 * @param input Where the canvas is and how to measure it.
 * @returns The pointer move and transform change handlers to wire onto the canvas.
 */
export const useCanvasPresence = ({ room, area, surfaceId, transformRef, surfaceRef, options }: CanvasPresenceInput): CanvasPresenceHandlers => {
    const client = useCollaborationClient(room, options);
    const lastScreenRef = useRef<Point | undefined>(undefined);

    const report = useCallback((screen: Point) => {
        reportCanvasPresence(client, screen, transformRef.current, area, surfaceId);
    }, [client, transformRef, area, surfaceId]);

    // Announced as soon as the canvas opens, so the others list this session under the right area and filter their
    // cursors correctly before the pointer has moved. After a reconnect the client replays the latest position itself.
    useEffect(() => {
        if (!client) return undefined;
        const bounds = surfaceRef.current?.getBoundingClientRect();
        report(lastScreenRef.current ?? { x: (bounds?.width ?? 0) / 2, y: (bounds?.height ?? 0) / 2 });
        return () => client.leaveArea();
    }, [client, report, surfaceRef]);

    const handlePointerMove = useCallback((event: { clientX: number; clientY: number }) => {
        const bounds = surfaceRef.current?.getBoundingClientRect();
        if (!bounds || bounds.width === 0 || bounds.height === 0) return;
        lastScreenRef.current = { x: event.clientX - bounds.left, y: event.clientY - bounds.top };
        report(lastScreenRef.current);
    }, [report, surfaceRef]);

    // Panning moves the content under a pointer that stands still, so its content position changes with it.
    const handleTransformChange = useCallback(() => {
        if (lastScreenRef.current) report(lastScreenRef.current);
    }, [report]);

    return { handlePointerMove, handleTransformChange };
};
