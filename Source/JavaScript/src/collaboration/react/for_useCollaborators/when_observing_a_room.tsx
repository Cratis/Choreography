// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, cleanup, renderHook } from '@testing-library/react';
import { StrictMode } from 'react';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { currentCollaboration } from '../../collaborationStore.js';
import { FakeCollaborationSocket } from '../../testing/FakeCollaborationSocket.js';
import { a_collaboration_room } from '../../testing/a_collaboration_room.js';
import { collaboratorInfoOf } from '../../testing/collaboratorInfoOf.js';
import { CollaborationConnectionState } from '../../CollaborationConnectionState.js';
import { useCollaborationConnectionState } from '../useCollaborationConnectionState.js';
import { useCollaborators } from '../useCollaborators.js';
import { useOwnOtherTabs } from '../useOwnOtherTabs.js';

describe('when observing a room', () => {
    let context: a_collaboration_room;

    beforeEach(() => {
        context = new a_collaboration_room();
    });

    afterEach(() => {
        cleanup();
        context.dispose();
    });

    describe('and there is no room yet', () => {
        it('should connect to nothing', () => {
            const { result } = renderHook(() => useCollaborators(undefined));
            expect(result.current).toEqual([]);
            expect(FakeCollaborationSocket.instances).toHaveLength(0);
        });
    });

    describe('and the room has others in it', () => {
        it('should deliver the collaborators as they arrive and update as they move', () => {
            const { result } = renderHook(() => useCollaborators('room'));
            expect(result.current).toEqual([]);

            context.deliverRoster([collaboratorInfoOf({ sessionId: 'a', userId: 'ada', x: 1, y: 2 })]);
            expect(result.current.map(collaborator => collaborator.sessionId)).toEqual(['a']);

            act(() => context.socket.deliver({ type: 'presence', sessionId: 'a', x: 5, y: 6, area: 'board', surfaceId: 'surface-1' }));
            expect(result.current[0].x).toBe(5);
        });

        it('should keep the same array while nothing changed', () => {
            const { result, rerender } = renderHook(() => useCollaborators('room'));
            context.deliverRoster([collaboratorInfoOf({ sessionId: 'a', userId: 'ada' })]);
            const before = result.current;
            rerender();
            expect(result.current).toBe(before);
        });

        it('should count the user\'s own other tabs apart from the collaborators', () => {
            const { result } = renderHook(() => ({ others: useCollaborators('room'), own: useOwnOtherTabs('room') }));
            context.deliverRoster([
                collaboratorInfoOf({ sessionId: 'a', userId: 'ada' }),
                collaboratorInfoOf({ sessionId: 'm', userId: 'me' })
            ]);
            expect(result.current.others).toHaveLength(1);
            expect(result.current.own).toBe(1);
        });

        it('should report the connection state', () => {
            const { result } = renderHook(() => useCollaborationConnectionState('room'));
            expect(result.current).toBe(CollaborationConnectionState.Connecting);
            context.deliverRoster([]);
            expect(result.current).toBe(CollaborationConnectionState.Connected);
        });
    });

    describe('and several hooks observe the same room', () => {
        it('should share a single connection', () => {
            renderHook(() => ({ a: useCollaborators('room'), b: useOwnOtherTabs('room'), c: useCollaborationConnectionState('room') }));
            expect(FakeCollaborationSocket.instances).toHaveLength(1);
        });
    });

    describe('and the components are unmounted', () => {
        it('should leave the room once the last one is gone', () => {
            const first = renderHook(() => useCollaborators('room'));
            const second = renderHook(() => useOwnOtherTabs('room'));
            first.unmount();
            expect(currentCollaboration()).toBeDefined();
            second.unmount();
            expect(currentCollaboration()).toBeUndefined();
            expect(context.socket.close).toHaveBeenCalled();
        });
    });

    describe('and the room changes', () => {
        it('should leave the old room and join the new one', () => {
            const { rerender } = renderHook(({ room }) => useCollaborators(room), { initialProps: { room: 'room-1' } });
            const first = context.socket;
            rerender({ room: 'room-2' });
            expect(first.close).toHaveBeenCalled();
            expect(new URL(context.socket.url).searchParams.get('room')).toBe('room-2');
        });
    });

    describe('and the component is mounted twice by strict mode', () => {
        it('should end up connected, with no connection left over from the discarded mount', () => {
            const { unmount } = renderHook(() => useCollaborators('room'), { wrapper: StrictMode });
            context.deliverRoster([collaboratorInfoOf({ sessionId: 'a', userId: 'ada' })]);

            const open = FakeCollaborationSocket.instances.filter(socket => socket.readyState === FakeCollaborationSocket.OPEN);
            expect(open).toHaveLength(1);
            expect(currentCollaboration()).toBeDefined();

            unmount();
            expect(currentCollaboration()).toBeUndefined();
        });
    });

    describe('and options are given', () => {
        it('should connect with them', () => {
            renderHook(() => useCollaborators('room', { path: '/live/presence' }));
            expect(new URL(context.socket.url).pathname).toBe('/live/presence');
        });
    });
});
