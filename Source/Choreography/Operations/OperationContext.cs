// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents what an operation definition's undo and redo rules see: the original command, the recorded
/// operation, and the host's services to read current state through.
/// </summary>
/// <typeparam name="TCommand">The type of the operation's forward command.</typeparam>
/// <param name="Command">The command the operation performed.</param>
/// <param name="Operation">The recorded operation.</param>
/// <param name="Services">Services of the host, for reading current state.</param>
public record OperationContext<TCommand>(TCommand Command, OperationEntry Operation, IServiceProvider Services)
{
    /// <summary>
    /// Get the state the host captured when it executed the operation, see <see cref="OperationExecutionResult.Memento"/>.
    /// </summary>
    /// <typeparam name="T">The type it was captured as.</typeparam>
    /// <returns>The state, or default when nothing was captured.</returns>
    public T? GetMemento<T>() =>
        Operation.Memento is null ? default : JsonSerializer.Deserialize<T>(Operation.Memento, OperationJson.Options);
}
