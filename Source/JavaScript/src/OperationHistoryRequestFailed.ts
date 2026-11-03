// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Thrown when a request could not be completed at all: the transport failed or the response was not understood.
 * A request the backend understood and refused is an OperationOutcome, not this.
 */
export class OperationHistoryRequestFailed extends Error {
    /**
     * Initializes a new instance of the OperationHistoryRequestFailed class.
     * @param message What went wrong.
     * @param status The HTTP status, when there was one.
     * @param cause The underlying error, when there was one.
     */
    constructor(message: string, readonly status?: number, cause?: unknown) {
        super(message, cause === undefined ? undefined : { cause });
        this.name = 'OperationHistoryRequestFailed';
    }
}
