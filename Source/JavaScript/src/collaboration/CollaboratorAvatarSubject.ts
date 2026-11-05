// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * What an avatar needs to know about the person it draws. Both a single collaborator session and a person
 * grouped from several sessions satisfy it.
 */
export interface CollaboratorAvatarSubject {
    /** The user the avatar belongs to. */
    userId: string;

    /** The name to show. */
    displayName: string;

    /** The person's color as a hex value without the leading `#`. */
    color: string;

    /** Whether the person has an uploaded avatar. */
    hasAvatar?: boolean;
}
