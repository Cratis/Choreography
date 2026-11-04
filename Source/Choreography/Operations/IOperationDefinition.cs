// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Defines the behavior of one kind of operation: how to reverse it, when that is safe, and how to reissue it.
/// Create them through <see cref="IOperationDefinitions.Define{TCommand}"/>.
/// </summary>
public interface IOperationDefinition
{
    /// <summary>
    /// Gets the kind of operation the definition describes.
    /// </summary>
    OperationKind Kind { get; }

    /// <summary>
    /// Gets the type of the forward command.
    /// </summary>
    Type CommandType { get; }

    /// <summary>
    /// Gets whether the definition says how to undo the operation.
    /// </summary>
    bool CanBeUndone { get; }

    /// <summary>
    /// Serialize a forward command so it can be stored as the operation's intent.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The serialized command.</returns>
    string SerializeIntent(object command);

    /// <summary>
    /// Evaluate against current state whether an operation can be undone.
    /// </summary>
    /// <param name="operation">The operation to undo.</param>
    /// <param name="services">Services of the host.</param>
    /// <returns>Null when it can be undone; otherwise why it cannot.</returns>
    Task<string?> EvaluateUndo(OperationEntry operation, IServiceProvider services);

    /// <summary>
    /// Create the command that reverses an operation.
    /// </summary>
    /// <param name="operation">The operation to undo.</param>
    /// <param name="services">Services of the host.</param>
    /// <returns>The inverse command.</returns>
    Task<object> CreateUndo(OperationEntry operation, IServiceProvider services);

    /// <summary>
    /// Evaluate against current state whether an undone operation can be redone.
    /// </summary>
    /// <param name="operation">The undone operation.</param>
    /// <param name="services">Services of the host.</param>
    /// <returns>Null when it can be redone; otherwise why it cannot.</returns>
    Task<string?> EvaluateRedo(OperationEntry operation, IServiceProvider services);

    /// <summary>
    /// Create the forward command that reissues an undone operation.
    /// </summary>
    /// <param name="operation">The undone operation.</param>
    /// <param name="services">Services of the host.</param>
    /// <returns>The forward command.</returns>
    Task<object> CreateRedo(OperationEntry operation, IServiceProvider services);
}
