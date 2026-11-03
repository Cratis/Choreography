// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Defines the registry of operation definitions.
/// </summary>
public interface IOperationDefinitions
{
    /// <summary>
    /// Define an operation performed by a forward command.
    /// </summary>
    /// <typeparam name="TCommand">The type of the forward command.</typeparam>
    /// <param name="kind">Optional kind; defaults to the full name of the command type.</param>
    /// <returns>The <see cref="OperationDefinition{TCommand}"/> to describe undo and redo on.</returns>
    OperationDefinition<TCommand> Define<TCommand>(OperationKind? kind = null)
        where TCommand : notnull;

    /// <summary>
    /// Find the definition of a kind of operation.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The definition, or null when the kind is not defined.</returns>
    IOperationDefinition? Find(OperationKind kind);

    /// <summary>
    /// Find the definition for a forward command type.
    /// </summary>
    /// <param name="commandType">The command type.</param>
    /// <returns>The definition, or null when the command is not defined.</returns>
    IOperationDefinition? Find(Type commandType);
}
