// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Watches a connection that the other end pings, and reports it stale when nothing has been heard for too
 * long. Call {@link ConnectionKeepAlive.recordActivity} whenever anything arrives; when the silence lasts
 * for the stale threshold, the callback runs once and the watch stops until it is started again.
 */
export class ConnectionKeepAlive {
    #timer?: ReturnType<typeof setInterval>;
    #lastActivity = Date.now();

    /**
     * Initializes a new instance of the ConnectionKeepAlive class.
     * @param intervalInMilliseconds How often the other end pings, which is also how often the watch checks.
     * @param onStale Called when the connection has been silent for the stale threshold.
     * @param staleAfterInMilliseconds How long the silence may last. Defaults to twice the interval.
     */
    constructor(
        readonly intervalInMilliseconds: number,
        readonly onStale: () => void,
        readonly staleAfterInMilliseconds: number = intervalInMilliseconds * 2) {
    }

    /** Whether the watch is running. */
    get isRunning(): boolean {
        return this.#timer !== undefined;
    }

    /** Starts watching, counting the silence from now. Starting a watch that is running restarts the count. */
    start(): void {
        this.stop();
        this.#lastActivity = Date.now();
        this.#timer = setInterval(() => this.#check(), this.intervalInMilliseconds);
    }

    /** Stops watching. */
    stop(): void {
        if (this.#timer !== undefined) {
            clearInterval(this.#timer);
            this.#timer = undefined;
        }
    }

    /** Records that the connection is alive. */
    recordActivity(): void {
        this.#lastActivity = Date.now();
    }

    #check(): void {
        if (Date.now() - this.#lastActivity >= this.staleAfterInMilliseconds) {
            this.stop();
            this.onStale();
        }
    }
}
