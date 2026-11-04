// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { OperationHistoryItem } from './OperationHistoryItem.js';
import { OperationHistoryRequestFailed } from './OperationHistoryRequestFailed.js';
import { OperationHistoryTransport } from './OperationHistoryTransport.js';
import { OperationOutcome } from './OperationOutcome.js';
import { OperationOutcomeKind } from './OperationOutcomeKind.js';
import { OperationRole } from './OperationRole.js';
import { OperationStatus } from './OperationStatus.js';

/**
 * Options for the HTTP transport.
 */
export interface HttpOperationHistoryTransportOptions {
    /**
     * The URL the host maps the operation engine under, without a trailing slash. For example '/api/operations'.
     */
    baseUrl: string;

    /**
     * The fetch implementation to use. Defaults to the global fetch.
     */
    fetch?: typeof fetch;

    /**
     * Headers to send with every request, for example authorization. May be computed per request.
     */
    headers?: HeadersInit | (() => HeadersInit | Promise<HeadersInit>);

    /**
     * Subscribes to changes the application learns about some other way. Left out, the transport cannot push.
     */
    subscribe?: OperationHistoryTransport['subscribe'];
}

/**
 * Creates a transport that talks to a host that exposes the Choreography operation engine over HTTP with JSON.
 *
 * The host maps three routes under the base URL to IOperationEngine:
 *
 * - GET {baseUrl}/history?scope=&actor= returns the history items, oldest first.
 * - POST {baseUrl}/undo with { scope, actor } returns an outcome.
 * - POST {baseUrl}/redo with { scope, actor } returns an outcome.
 *
 * A refused undo or redo is a normal answer with a Blocked outcome, not an HTTP error. Property names are
 * camelCase and enum values are strings, which is what ASP.NET Core writes with its web defaults and a string
 * enum converter.
 * @param options The transport options.
 * @returns The transport.
 */
export const createHttpTransport = (options: HttpOperationHistoryTransportOptions): OperationHistoryTransport => {
    const baseUrl = options.baseUrl.replace(/\/+$/, '');

    const send = async (path: string, init: RequestInit, signal?: AbortSignal): Promise<unknown> => {
        const doFetch = options.fetch ?? globalThis.fetch;
        const extraHeaders = typeof options.headers === 'function' ? await options.headers() : options.headers;
        const headers = new Headers(extraHeaders);
        headers.set('Accept', 'application/json');
        if (init.body !== undefined) {
            headers.set('Content-Type', 'application/json');
        }

        let response: Response;
        try {
            response = await doFetch(`${baseUrl}${path}`, { ...init, headers, signal });
        } catch (error) {
            throw new OperationHistoryRequestFailed('The operation history request could not be sent.', undefined, error);
        }

        if (!response.ok) {
            throw new OperationHistoryRequestFailed(`The operation history request failed with status ${response.status}.`, response.status);
        }

        try {
            return await response.json();
        } catch (error) {
            throw new OperationHistoryRequestFailed('The operation history response was not valid JSON.', response.status, error);
        }
    };

    const post = async (path: string, scope: string, actor: string, signal?: AbortSignal) =>
        parseOutcome(await send(path, { method: 'POST', body: JSON.stringify({ scope, actor }) }, signal));

    return {
        getHistory: async (scope, actor, signal) => {
            const query = new URLSearchParams({ scope, actor });
            return parseHistory(await send(`/history?${query}`, { method: 'GET' }, signal));
        },
        undo: (scope, actor, signal) => post('/undo', scope, actor, signal),
        redo: (scope, actor, signal) => post('/redo', scope, actor, signal),
        subscribe: options.subscribe
    };
};

const notUnderstood = (what: string): OperationHistoryRequestFailed =>
    new OperationHistoryRequestFailed(`The operation history response was not understood: ${what}.`);

const pick = <T extends string>(values: Record<string, T>, value: unknown, what: string): T => {
    const match = typeof value === 'string'
        ? Object.values(values).find(candidate => candidate.toLowerCase() === value.toLowerCase())
        : undefined;
    if (!match) throw notUnderstood(`unknown ${what} '${String(value)}'`);
    return match;
};

const text = (value: unknown, what: string): string => {
    if (typeof value !== 'string') throw notUnderstood(`${what} is missing`);
    return value;
};

const optionalText = (value: unknown): string | undefined => typeof value === 'string' && value.length > 0 ? value : undefined;

/**
 * Reads a history response into history items, rejecting anything that is not understood.
 * @param payload The parsed JSON body.
 * @returns The history items.
 */
export const parseHistory = (payload: unknown): OperationHistoryItem[] => {
    if (!Array.isArray(payload)) throw notUnderstood('expected a list of operations');

    return payload.map((raw: Record<string, unknown>) => {
        const started = new Date(text(raw.started, 'started'));
        if (Number.isNaN(started.getTime())) throw notUnderstood('started is not a date');
        const sequenceNumbers = raw.eventSequenceNumbers ?? [];
        if (!Array.isArray(sequenceNumbers) || sequenceNumbers.some(number => typeof number !== 'number')) {
            throw notUnderstood('eventSequenceNumbers is not a list of numbers');
        }

        return {
            id: text(raw.id, 'id'),
            position: Number(raw.position),
            scope: text(raw.scope, 'scope'),
            actor: text(raw.actor, 'actor'),
            kind: text(raw.kind, 'kind'),
            role: pick(OperationRole, raw.role, 'role'),
            status: pick(OperationStatus, raw.status, 'status'),
            eventSequenceNumbers: sequenceNumbers as number[],
            started,
            target: optionalText(raw.target),
            canUndo: raw.canUndo === true,
            canRedo: raw.canRedo === true,
            blockedReason: optionalText(raw.blockedReason)
        };
    });
};

/**
 * Reads an undo or redo response into an outcome, rejecting anything that is not understood.
 * @param payload The parsed JSON body.
 * @returns The outcome.
 */
export const parseOutcome = (payload: unknown): OperationOutcome => {
    if (typeof payload !== 'object' || payload === null) throw notUnderstood('expected an outcome');
    const raw = payload as Record<string, unknown>;

    return {
        kind: pick(OperationOutcomeKind, raw.kind, 'outcome'),
        operationId: optionalText(raw.operationId),
        reason: optionalText(raw.reason)
    };
};
