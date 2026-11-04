// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Defines how a host executes the commands the engine issues. This is the engine's only link to the outside
/// world, which keeps it independent of transport and of Chronicle.
/// </summary>
public interface IOperationExecutor
{
    /// <summary>
    /// Execute a command as part of an operation.
    /// </summary>
    /// <param name="command">The command - a forward command, or an inverse supplied by an operation definition.</param>
    /// <param name="context">The operation it is executed for.</param>
    /// <returns>The <see cref="OperationExecutionResult"/>, including the sequence numbers of the events it produced.</returns>
    Task<OperationExecutionResult> Execute(object command, OperationExecutionContext context);
}
