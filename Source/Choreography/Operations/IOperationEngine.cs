// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Defines the undo/redo engine. It executes operations through the host's <see cref="IOperationExecutor"/>,
/// records them in the <see cref="IOperationHistory"/>, and reverses them as new operations - history is
/// never rewritten.
/// </summary>
public interface IOperationEngine
{
    /// <summary>
    /// Perform an operation defined through <see cref="IOperationDefinitions"/>. A successful operation clears the
    /// actor's redo stack in the scope.
    /// </summary>
    /// <typeparam name="TCommand">The type of the forward command.</typeparam>
    /// <param name="scope">The scope.</param>
    /// <param name="actor">The actor.</param>
    /// <param name="command">The command.</param>
    /// <returns>The <see cref="OperationOutcome"/>.</returns>
    Task<OperationOutcome> Execute<TCommand>(OperationScope scope, OperationActor actor, TCommand command)
        where TCommand : notnull;

    /// <summary>
    /// Undo the actor's latest active operation in the scope. When it is blocked the outcome says why and no
    /// older operation is tried.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="actor">The actor.</param>
    /// <returns>The <see cref="OperationOutcome"/>.</returns>
    Task<OperationOutcome> Undo(OperationScope scope, OperationActor actor);

    /// <summary>
    /// Redo the actor's most recently undone operation in the scope, after validating against current state.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="actor">The actor.</param>
    /// <returns>The <see cref="OperationOutcome"/>.</returns>
    Task<OperationOutcome> Redo(OperationScope scope, OperationActor actor);

    /// <summary>
    /// Get the actor's history in the scope, in order, with undo and redo eligibility.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="actor">The actor.</param>
    /// <returns>The history items, oldest first.</returns>
    Task<IReadOnlyList<OperationHistoryItem>> GetHistory(OperationScope scope, OperationActor actor);
}
