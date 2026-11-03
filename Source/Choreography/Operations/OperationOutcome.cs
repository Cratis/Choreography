// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the outcome of an execute, undo or redo request. A plain record, ready to serialize to a client.
/// </summary>
/// <param name="Kind">How the request ended.</param>
/// <param name="OperationId">The operation that was recorded, when one was.</param>
/// <param name="Reason">Why the request was blocked or failed.</param>
public record OperationOutcome(OperationOutcomeKind Kind, Guid? OperationId = null, string? Reason = null)
{
    /// <summary>
    /// Gets the outcome for when there is nothing to undo.
    /// </summary>
    public static OperationOutcome NothingToUndo { get; } = new(OperationOutcomeKind.NothingToUndo);

    /// <summary>
    /// Gets the outcome for when there is nothing to redo.
    /// </summary>
    public static OperationOutcome NothingToRedo { get; } = new(OperationOutcomeKind.NothingToRedo);

    /// <summary>
    /// Create a succeeded outcome.
    /// </summary>
    /// <param name="operationId">The operation that was recorded.</param>
    /// <returns>The outcome.</returns>
    public static OperationOutcome Succeeded(OperationId operationId) => new(OperationOutcomeKind.Succeeded, operationId.Value);

    /// <summary>
    /// Create a blocked outcome.
    /// </summary>
    /// <param name="reason">Why it is blocked.</param>
    /// <returns>The outcome.</returns>
    public static OperationOutcome Blocked(string reason) => new(OperationOutcomeKind.Blocked, null, reason);

    /// <summary>
    /// Create a failed outcome.
    /// </summary>
    /// <param name="operationId">The operation that was recorded as failed.</param>
    /// <param name="reason">Why it failed.</param>
    /// <returns>The outcome.</returns>
    public static OperationOutcome Failed(OperationId operationId, string reason) => new(OperationOutcomeKind.Failed, operationId.Value, reason);
}
