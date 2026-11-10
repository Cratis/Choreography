// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useEffect, useRef, useState, type ReactNode, type RefObject } from 'react';
import { CollaborationConnectionState } from '../CollaborationConnectionState.js';
import type { CollaborationStoreOptions } from '../CollaborationStoreOptions.js';
import { fillCount } from '../fillCount.js';
import { groupCollaborators } from '../groupCollaborators.js';
import { CollaboratorAvatar } from './CollaboratorAvatar.js';
import { defaultCollaborationIndicatorLabels, type CollaborationIndicatorLabels } from './CollaborationIndicatorLabels.js';
import type { CollaborationIndicatorTriggerState } from './CollaborationIndicatorTriggerState.js';
import { CollaborationReconnectIndicator } from './CollaborationReconnectIndicator.js';
import type { RenderCollaboratorAvatar } from './RenderCollaboratorAvatar.js';
import { collaborationButton } from './collaborationButton.js';
import { useCollaborationConnectionState } from './useCollaborationConnectionState.js';
import { useCollaborators } from './useCollaborators.js';
import { useOwnOtherTabs } from './useOwnOtherTabs.js';

/** The size in pixels of the avatars in the panel. */
const PANEL_AVATAR_SIZE = 28;

/**
 * Properties for {@link CollaborationIndicator}.
 */
export interface CollaborationIndicatorProperties {
    /** The room whose collaborators to show. */
    room: string;

    /** Names for the areas collaborators are in, keyed by area. An area without a name is shown as it is. */
    areaLabels?: Record<string, string>;

    /** Overrides for the user-visible strings. */
    labels?: Partial<CollaborationIndicatorLabels>;

    /** Draws the avatars in the panel. Defaults to the person's initials on their color. */
    renderAvatar?: RenderCollaboratorAvatar;

    /**
     * Draws the button that opens the panel, so a host can use its own toolbar button. The indicator keeps
     * the button's container, the count badge and the panel. Defaults to a plain button with a users icon.
     */
    renderTrigger?: (state: CollaborationIndicatorTriggerState) => ReactNode;

    /** Which side of the trigger the panel lines up with. Defaults to `end`. */
    panelAlignment?: 'start' | 'end';

    /** How to reach the backend; applied when this component is the first to connect. */
    options?: CollaborationStoreOptions;

    /** An extra class name for the indicator's container. */
    className?: string;
}

const UsersIcon = () => (
    <svg viewBox='0 0 24 24' width='1em' height='1em' fill='none' stroke='currentColor' strokeWidth='2' strokeLinecap='round' strokeLinejoin='round' aria-hidden='true'>
        <path d='M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2' />
        <circle cx='9' cy='7' r='4' />
        <path d='M23 21v-2a4 4 0 0 0-3-3.87' />
        <path d='M16 3.13a4 4 0 0 1 0 7.75' />
    </svg>
);

/** Closes the open panel on a press outside the container or on Escape; listens only while the panel is open. */
const useCloseOnOutsidePressOrEscape = (
    isOpen: boolean,
    setIsOpen: (isOpen: boolean) => void,
    containerRef: RefObject<HTMLDivElement | null>) => {
    useEffect(() => {
        if (!isOpen) return undefined;

        const closeOnOutsidePress = (event: Event) => {
            if (event.target instanceof Node && !containerRef.current?.contains(event.target)) {
                setIsOpen(false);
            }
        };
        const closeOnEscape = (event: KeyboardEvent) => {
            if (event.key === 'Escape') setIsOpen(false);
        };
        document.addEventListener('pointerdown', closeOnOutsidePress);
        document.addEventListener('keydown', closeOnEscape);
        return () => {
            document.removeEventListener('pointerdown', closeOnOutsidePress);
            document.removeEventListener('keydown', closeOnEscape);
        };
    }, [isOpen]);
};

interface CollaboratorPanelItemProperties {
    person: ReturnType<typeof groupCollaborators>[number];
    text: typeof defaultCollaborationIndicatorLabels;
    areaLabel: string | undefined;
    avatar: ReactNode;
}

const CollaboratorPanelItem = ({ person, text, areaLabel, avatar }: CollaboratorPanelItemProperties) => (
    <div className={`collaboration-indicator__item${person.isIdle ? ' collaboration-indicator__item--idle' : ''}`}>
        <span className='collaboration-indicator__avatar'>{avatar}</span>
        <div className='collaboration-indicator__item-info'>
            <span className='collaboration-indicator__item-name'>
                {person.displayName}
                {person.tabs > 1 && (
                    <span className='collaboration-indicator__item-note'>{fillCount(text.tabs, person.tabs)}</span>
                )}
                {person.isIdle && (
                    <span className='collaboration-indicator__item-note'>{text.idle}</span>
                )}
            </span>
            {areaLabel && <span className='collaboration-indicator__item-area'>{areaLabel}</span>}
        </div>
    </div>
);

/**
 * Shows who else is here: a button with a count badge that opens a panel listing the people, one entry per
 * person with the area they are in and how many tabs they have open. It does not depend on a design system -
 * the panel is part of the indicator, closes on an outside click or Escape, and a host that wants its own
 * button supplies it through `renderTrigger`.
 *
 * While the connection is down for long enough the button turns red and struck through, and a status message
 * is announced to assistive technology.
 * @param properties What to show.
 * @returns The indicator.
 */
export const CollaborationIndicator = ({
    room,
    areaLabels,
    labels,
    renderAvatar,
    renderTrigger,
    panelAlignment = 'end',
    options,
    className
}: CollaborationIndicatorProperties) => {
    const text = { ...defaultCollaborationIndicatorLabels, ...labels };

    // One entry per PERSON, not per tab: a user with two tabs open is listed once with a tab count, and the
    // signed-in user's own other tabs are never listed as a collaborator (see CollaborationClient).
    const people = groupCollaborators(useCollaborators(room, options));
    const ownOtherTabs = useOwnOtherTabs(room, options);
    const connectionState = useCollaborationConnectionState(room, options);
    const containerRef = useRef<HTMLDivElement>(null);
    const [isOpen, setIsOpen] = useState(false);

    useCloseOnOutsidePressOrEscape(isOpen, setIsOpen, containerRef);

    const button = collaborationButton(connectionState, text.title, text.reconnecting);
    const toggle = () => setIsOpen(current => !current);
    const triggerState: CollaborationIndicatorTriggerState = {
        toggle,
        isOpen,
        title: button.title,
        connectionState,
        count: people.length
    };
    const avatar: RenderCollaboratorAvatar = renderAvatar ?? ((subject, size) => <CollaboratorAvatar subject={subject} size={size} />);

    return (
        <div ref={containerRef} className={className ? `${button.className} ${className}` : button.className}>
            {renderTrigger ? renderTrigger(triggerState) : (
                <button
                    type='button'
                    className='collaboration-indicator__button'
                    title={button.title}
                    aria-label={button.title}
                    aria-haspopup='dialog'
                    aria-expanded={isOpen}
                    onClick={toggle}>
                    <span className='collaboration-indicator__icon'><UsersIcon /></span>
                </button>
            )}
            {people.length > 0 && (
                <span className='collaboration-indicator__count-badge' aria-label={fillCount(text.collaborators, people.length)}>
                    {people.length}
                </span>
            )}
            <CollaborationReconnectIndicator state={connectionState} label={text.reconnecting} />

            {isOpen && (
                <div
                    className={`collaboration-indicator__panel collaboration-indicator__panel--${panelAlignment}`}
                    role='dialog'
                    aria-label={text.title}>
                    <div className='collaboration-indicator__header'>{text.title}</div>

                    {connectionState === CollaborationConnectionState.Reconnecting && (
                        <div className='collaboration-indicator__own-tabs'>{text.reconnecting}</div>
                    )}

                    {connectionState !== CollaborationConnectionState.Connected ? null : people.length === 0 ? (
                        <div className='collaboration-indicator__list'>
                            <div className='collaboration-indicator__item'>
                                <span className='collaboration-indicator__item-name collaboration-indicator__item-name--secondary'>
                                    {text.onlyYou}
                                </span>
                            </div>
                        </div>
                    ) : (
                        <div className='collaboration-indicator__list'>
                            {people.map(person => (
                                <CollaboratorPanelItem
                                    key={person.userId}
                                    person={person}
                                    text={text}
                                    areaLabel={person.area ? areaLabels?.[person.area] ?? person.area : undefined}
                                    avatar={avatar(person, PANEL_AVATAR_SIZE)} />
                            ))}
                        </div>
                    )}
                    {connectionState === CollaborationConnectionState.Connected && ownOtherTabs > 0 && (
                        <div className='collaboration-indicator__own-tabs'>{fillCount(text.ownTabs, ownOtherTabs)}</div>
                    )}
                </div>
            )}
        </div>
    );
};
