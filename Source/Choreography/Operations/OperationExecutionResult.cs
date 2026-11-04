// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the result of the host executing a command for an operation.
/// </summary>
/// <param name="Succeeded">Whether the command was executed.</param>
/// <param name="EventSequenceNumbers">The sequence numbers of the events it produced, zero or more.</param>
/// <param name="Memento">
/// Optional state worth keeping to reverse the command later - for example the value it replaced. It must be
/// serializable with System.Text.Json and is handed back through <see cref="OperationContext{TCommand}.GetMemento{T}"/>.
/// </param>
/// <param name="Error">Why it did not succeed.</param>
public record OperationExecutionResult(bool Succeeded, IReadOnlyList<ulong> EventSequenceNumbers, object? Memento = null, string? Error = null)
{
    /// <summary>
    /// Create a successful result.
    /// </summary>
    /// <param name="eventSequenceNumbers">The sequence numbers of the events produced.</param>
    /// <param name="memento">Optional state to keep for reversing the command.</param>
    /// <returns>The result.</returns>
    public static OperationExecutionResult Success(IEnumerable<ulong> eventSequenceNumbers, object? memento = null) =>
        new(true, [.. eventSequenceNumbers], memento);

    /// <summary>
    /// Create a failed result.
    /// </summary>
    /// <param name="error">Why the command was not executed.</param>
    /// <returns>The result.</returns>
    public static OperationExecutionResult Failure(string error) => new(false, [], null, error);
}
