// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useMemo, useRef } from 'react';
import type { CanvasTransform } from '../CanvasTransform.js';
import type { CollaborationStoreOptions } from '../CollaborationStoreOptions.js';
import type { CursorParts } from '../CursorParts.js';
import type { CursorSample } from '../CursorSample.js';
import type { SelfSuspendingFrameLoop } from '../SelfSuspendingFrameLoop.js';
import { createSelfSuspendingFrameLoop } from '../createSelfSuspendingFrameLoop.js';
import { positionAt } from '../cursorPath.js';
import { hasLiveSamples } from '../hasLiveSamples.js';
import { positionCursor } from '../positionCursor.js';
import { visibleCollaboratorCursors } from '../visibleCollaboratorCursors.js';
import type { CanvasTransformActivity } from './CanvasTransformActivity.js';
import { ARROW_CLASS, CollaboratorCursor, OUTLINE_CLASS, PILL_CLASS } from './CollaboratorCursor.js';
import type { RenderCollaboratorAvatar } from './RenderCollaboratorAvatar.js';
import { useCollaborators } from './useCollaborators.js';

/**
 * How far behind "now" cursors are rendered. The buffer gives the interpolator a segment to walk through,
 * turning the ~30 ms presence samples into one continuous curved motion.
 */
export const RENDER_DELAY_MS = 120;

/** Samples older than this are irrelevant to the curve and are pruned. */
export const SAMPLE_LIFETIME_MS = 2_000;

/**
 * Properties for {@link CollaboratorCursors}.
 */
export interface CollaboratorCursorsProperties {
    /** The room whose collaborators to render. */
    room: string;

    /** The area of the application this surface represents, for example `board`. */
    area: string;

    /** The identifier of the surface whose cursors should be shown. */
    surfaceId: string;

    /**
     * The live local canvas transform, used to map surface content coordinates to the screen. A reference
     * rather than a value so the per-frame pan/zoom path never has to re-render the owning surface - the
     * follower reads the current transform inside its own animation frame.
     */
    transformRef: { readonly current: CanvasTransform | null | undefined };

    /**
     * Announces that the canvas transform moved. Without it the cursors still follow presence, but a pan or
     * zoom that happens while nobody else moves is only picked up on the next presence update.
     */
    transformActivity?: CanvasTransformActivity;

    /** Attributes put on every cursor's arrow and pill elements, for example to mark them as layers for a compositing host. */
    layerProps?: Record<string, string>;

    /** Draws the avatar in each cursor's pill. Defaults to the collaborator's initials on their color. */
    renderAvatar?: RenderCollaboratorAvatar;

    /** How to reach the backend; applied when this component is the first to connect. */
    options?: CollaborationStoreOptions;

    /** An extra class name for the overlay. */
    className?: string;
}

/**
 * Renders the other collaborators' live cursors over a surface. Subscribes to presence itself (via the
 * collaboration WebSocket store), so cursor updates re-render only this small overlay - never the surface.
 * Remote positions arrive in surface content coordinates; an animation-frame follower renders each cursor a
 * hair behind real time, gliding through the recent samples on a Catmull-Rom curve and mapping through the
 * LIVE local pan/zoom every frame - so motion is smooth rather than jumpy and cursors track correctly even
 * while the local user pans.
 *
 * A collaborator who has moved outside the local viewport is not dropped: {@link positionCursor} parks their
 * pill against the nearest edge and aims the arrow the way the local user would pan.
 *
 * The overlay fills its nearest positioned ancestor and lets pointer events through.
 * @param properties What to render and where.
 * @returns The overlay.
 */
export const CollaboratorCursors = ({
    room,
    area,
    surfaceId,
    transformRef,
    transformActivity,
    layerProps,
    renderAvatar,
    options,
    className
}: CollaboratorCursorsProperties) => {
    const collaborators = useCollaborators(room, options);

    // Memoized on the store's own stable snapshot reference (useCollaborators only returns a new array when
    // the roster genuinely changed) rather than recomputed by value on every render - an unstable array here
    // fans out into every effect and ref bookkeeping loop below that iterates it.
    const visibleCollaborators = useMemo(
        () => visibleCollaboratorCursors(collaborators, area, surfaceId),
        [collaborators, area, surfaceId]);

    const containerRef = useRef<HTMLDivElement>(null);
    const viewportRef = useRef({ width: 0, height: 0 });
    const samplesRef = useRef(new Map<string, CursorSample[]>());
    const nodesRef = useRef(new Map<string, CursorParts>());
    const refCallbacksRef = useRef(new Map<string, (node: HTMLDivElement | null) => void>());

    // Append each collaborator's latest position to their sample buffer as it arrives.
    for (const collaborator of visibleCollaborators) {
        let samples = samplesRef.current.get(collaborator.sessionId);
        if (!samples) samplesRef.current.set(collaborator.sessionId, (samples = []));
        const last = samples[samples.length - 1];
        if (!last || last.x !== collaborator.x || last.y !== collaborator.y) {
            samples.push({ x: collaborator.x, y: collaborator.y, time: performance.now() });
        }
    }

    // The viewport off-screen collaborators are parked against. Observed rather than measured in the
    // follower, so the per-frame path never forces a layout.
    useEffect(() => {
        const container = containerRef.current;
        if (!container) return;
        const measure = () => {
            viewportRef.current = { width: container.clientWidth, height: container.clientHeight };
        };
        measure();
        if (typeof ResizeObserver === 'undefined') return;
        const observer = new ResizeObserver(measure);
        observer.observe(container);
        return () => observer.disconnect();
    }, []);

    // The follower: while a cursor still has motion to render, evaluate each cursor's curve slightly in the
    // past and position its elements directly - no React state. Self-suspending rather than a permanent
    // per-frame loop: once every buffer has been walked to its newest sample the loop stops, and it wakes
    // again when a new sample arrives (a presence update re-renders this component) or the local pan/zoom
    // moves (the host announces it through transformActivity) - an idle surface positions nothing.
    const positionCursors = (now: number): boolean => {
        const { zoom, pan } = transformRef.current ?? { zoom: 1, pan: { x: 0, y: 0 } };
        const { width, height } = viewportRef.current;
        for (const [sessionId, parts] of nodesRef.current) {
            const samples = samplesRef.current.get(sessionId);
            if (!samples?.length) continue;
            while (samples.length > 2 && samples[0].time < now - SAMPLE_LIFETIME_MS) samples.shift();
            const position = positionAt(samples, now - RENDER_DELAY_MS);
            if (!position) continue;
            positionCursor(parts, position.x * zoom + pan.x, position.y * zoom + pan.y, width, height);
        }
        return hasLiveSamples(samplesRef.current.values(), now - RENDER_DELAY_MS);
    };
    const positionCursorsRef = useRef(positionCursors);
    positionCursorsRef.current = positionCursors;

    const followerRef = useRef<SelfSuspendingFrameLoop | null>(null);
    followerRef.current ??= createSelfSuspendingFrameLoop(now => positionCursorsRef.current(now));
    const follower = followerRef.current;

    useEffect(() => {
        const unsubscribe = transformActivity?.subscribe(() => follower.wake());
        return () => {
            unsubscribe?.();
            follower.stop();
        };
    }, [follower, transformActivity]);

    // Every commit is a potential new sample or a new cursor node (presence updates re-render this
    // component) - make sure the follower runs at least one frame to walk it.
    useEffect(() => {
        follower.wake();
    });

    // Ref callbacks must be IDENTITY-STABLE per session: React invokes the previous callback with null
    // whenever the callback identity changes, so a per-render closure would tear down (and wipe the sample
    // buffer of) every cursor on every presence update - freezing them at their first position. Buffers of
    // departed collaborators are pruned below instead.
    const registerNode = (sessionId: string) => {
        let callback = refCallbacksRef.current.get(sessionId);
        if (!callback) {
            callback = node => {
                const arrow = node?.querySelector<HTMLElement>(`.${ARROW_CLASS}`);
                const pill = node?.querySelector<HTMLElement>(`.${PILL_CLASS}`);
                if (arrow && pill) {
                    const outline = node?.querySelector<SVGPathElement>(`.${OUTLINE_CLASS}`) ?? null;

                    // The outline is rendered in its on-screen shape, so the recorded curvature has to say
                    // so - starting at zero would leave a cursor that opens on screen never redrawn.
                    nodesRef.current.set(sessionId, { arrow, pill, outline, tailCurvature: 1, pillWidth: 0, pillHeight: 0 });
                } else {
                    nodesRef.current.delete(sessionId);
                }
            };
            refCallbacksRef.current.set(sessionId, callback);
        }
        return callback;
    };

    const activeSessions = new Set(visibleCollaborators.map(collaborator => collaborator.sessionId));
    for (const sessionId of [...samplesRef.current.keys()]) {
        if (!activeSessions.has(sessionId)) {
            samplesRef.current.delete(sessionId);
            refCallbacksRef.current.delete(sessionId);
        }
    }

    return (
        <div
            ref={containerRef}
            className={className ? `collaborator-cursors ${className}` : 'collaborator-cursors'}
            aria-hidden='true'
        >
            {visibleCollaborators.map(collaborator => (
                <CollaboratorCursor
                    key={collaborator.sessionId}
                    collaborator={collaborator}
                    nodeRef={registerNode(collaborator.sessionId)}
                    layerProps={layerProps}
                    renderAvatar={renderAvatar}
                />
            ))}
        </div>
    );
};
