// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * The state of the connection to the collaboration backend, as far as presence is concerned.
 */
export enum CollaborationConnectionState {
    /** Connecting, or briefly re-connecting. Nothing is shown to the user in this state. */
    Connecting,

    /** The backend confirmed the join by sending the roster. */
    Connected,

    /** The connection has been down long enough that the user should be told. */
    Reconnecting,
}
