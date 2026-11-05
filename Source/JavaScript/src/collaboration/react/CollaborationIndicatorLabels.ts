// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Every user-visible string of the {@link CollaborationIndicator}. Labels with a `{count}` placeholder get
 * the number put in place of it.
 */
export interface CollaborationIndicatorLabels {
    /** The trigger's tooltip and the panel's heading. */
    title: string;

    /** Shown in the panel when nobody else is here. */
    onlyYou: string;

    /** Shown beside a person whose every tab is idle. */
    idle: string;

    /** Shown beside a person with several tabs open. Receives the number of tabs as `{count}`. */
    tabs: string;

    /** Shown at the foot of the panel when the user has other tabs open. Receives their number as `{count}`. */
    ownTabs: string;

    /** The trigger's tooltip while reconnecting, and the status announced to assistive technology. */
    reconnecting: string;

    /** The badge's accessible name. Receives the number of collaborators as `{count}`. */
    collaborators: string;
}

/** The English labels, used for whatever a host does not override. */
export const defaultCollaborationIndicatorLabels: CollaborationIndicatorLabels = {
    title: 'Active collaborators',
    onlyYou: 'Only you',
    idle: '(idle)',
    tabs: '{count} tabs',
    ownTabs: 'You are also open in {count} other tab(s)',
    reconnecting: 'Reconnecting collaboration…',
    collaborators: '{count} collaborators'
};
