// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

#pragma warning disable RCS1047 // The Async variants return the definition for fluent chaining; they take async delegates.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the fluent definition of one kind of operation, performed by a forward command.
/// </summary>
/// <typeparam name="TCommand">The type of the forward command.</typeparam>
/// <param name="kind">The kind of operation.</param>
public class OperationDefinition<TCommand>(OperationKind kind) : IOperationDefinition
    where TCommand : notnull
{
    const string DefaultUndoBlocked = "The current state no longer allows this operation to be undone";
    const string DefaultRedoBlocked = "The current state no longer allows this operation to be redone";

    readonly List<(Func<OperationContext<TCommand>, Task<bool>> Predicate, string Reason)> _undoRules = [];
    readonly List<(Func<OperationContext<TCommand>, Task<bool>> Predicate, string Reason)> _redoRules = [];
    Func<OperationContext<TCommand>, Task<object>>? _undo;
    Func<OperationContext<TCommand>, Task<TCommand>>? _redo;

    /// <inheritdoc/>
    public OperationKind Kind => kind;

    /// <inheritdoc/>
    public Type CommandType => typeof(TCommand);

    /// <inheritdoc/>
    public bool CanBeUndone => _undo is not null;

    /// <summary>
    /// Supply the inverse command that reverses the operation. The engine never infers it.
    /// </summary>
    /// <param name="inverse">Creates the inverse command.</param>
    /// <returns>The definition for continuation.</returns>
    public OperationDefinition<TCommand> UndoWith(Func<OperationContext<TCommand>, object> inverse)
    {
        _undo = context => Task.FromResult(inverse(context));
        return this;
    }

    /// <summary>
    /// Supply the inverse command that reverses the operation, asynchronously.
    /// </summary>
    /// <param name="inverse">Creates the inverse command.</param>
    /// <returns>The definition for continuation.</returns>
    public OperationDefinition<TCommand> UndoWithAsync(Func<OperationContext<TCommand>, Task<object>> inverse)
    {
        _undo = inverse;
        return this;
    }

    /// <summary>
    /// Add a rule that must hold in the current state for the operation to be undone.
    /// </summary>
    /// <param name="predicate">Evaluated against current state.</param>
    /// <param name="reason">Why the undo is blocked when the rule does not hold.</param>
    /// <returns>The definition for continuation.</returns>
    public OperationDefinition<TCommand> CanUndoWhen(Func<OperationContext<TCommand>, bool> predicate, string reason = DefaultUndoBlocked) =>
        CanUndoWhenAsync(context => Task.FromResult(predicate(context)), reason);

    /// <summary>
    /// Add an asynchronous rule that must hold in the current state for the operation to be undone.
    /// </summary>
    /// <param name="predicate">Evaluated against current state.</param>
    /// <param name="reason">Why the undo is blocked when the rule does not hold.</param>
    /// <returns>The definition for continuation.</returns>
    public OperationDefinition<TCommand> CanUndoWhenAsync(Func<OperationContext<TCommand>, Task<bool>> predicate, string reason = DefaultUndoBlocked)
    {
        _undoRules.Add((predicate, reason));
        return this;
    }

    /// <summary>
    /// Supply the forward command that reissues the operation. Without it the original command is reissued.
    /// </summary>
    /// <param name="forward">Creates the forward command.</param>
    /// <returns>The definition for continuation.</returns>
    public OperationDefinition<TCommand> RedoWith(Func<OperationContext<TCommand>, TCommand> forward)
    {
        _redo = context => Task.FromResult(forward(context));
        return this;
    }

    /// <summary>
    /// Supply the forward command that reissues the operation, asynchronously.
    /// </summary>
    /// <param name="forward">Creates the forward command.</param>
    /// <returns>The definition for continuation.</returns>
    public OperationDefinition<TCommand> RedoWithAsync(Func<OperationContext<TCommand>, Task<TCommand>> forward)
    {
        _redo = forward;
        return this;
    }

    /// <summary>
    /// Add a rule that must hold in the current state for the undone operation to be redone.
    /// </summary>
    /// <param name="predicate">Evaluated against current state.</param>
    /// <param name="reason">Why the redo is blocked when the rule does not hold.</param>
    /// <returns>The definition for continuation.</returns>
    public OperationDefinition<TCommand> CanRedoWhen(Func<OperationContext<TCommand>, bool> predicate, string reason = DefaultRedoBlocked) =>
        CanRedoWhenAsync(context => Task.FromResult(predicate(context)), reason);

    /// <summary>
    /// Add an asynchronous rule that must hold in the current state for the undone operation to be redone.
    /// </summary>
    /// <param name="predicate">Evaluated against current state.</param>
    /// <param name="reason">Why the redo is blocked when the rule does not hold.</param>
    /// <returns>The definition for continuation.</returns>
    public OperationDefinition<TCommand> CanRedoWhenAsync(Func<OperationContext<TCommand>, Task<bool>> predicate, string reason = DefaultRedoBlocked)
    {
        _redoRules.Add((predicate, reason));
        return this;
    }

    /// <inheritdoc/>
    public string SerializeIntent(object command) => JsonSerializer.Serialize((TCommand)command, OperationJson.Options);

    /// <inheritdoc/>
    public Task<string?> EvaluateUndo(OperationEntry operation, IServiceProvider services) =>
        Evaluate(_undoRules, operation, services);

    /// <inheritdoc/>
    public async Task<object> CreateUndo(OperationEntry operation, IServiceProvider services) =>
        await _undo!(CreateContext(operation, services));

    /// <inheritdoc/>
    public Task<string?> EvaluateRedo(OperationEntry operation, IServiceProvider services) =>
        Evaluate(_redoRules, operation, services);

    /// <inheritdoc/>
    public async Task<object> CreateRedo(OperationEntry operation, IServiceProvider services)
    {
        var context = CreateContext(operation, services);
        return _redo is null ? context.Command : await _redo(context);
    }

    static OperationContext<TCommand> CreateContext(OperationEntry operation, IServiceProvider services) =>
        new(JsonSerializer.Deserialize<TCommand>(operation.Intent!, OperationJson.Options)!, operation, services);

    static async Task<string?> Evaluate(
        List<(Func<OperationContext<TCommand>, Task<bool>> Predicate, string Reason)> rules,
        OperationEntry operation,
        IServiceProvider services)
    {
        var context = CreateContext(operation, services);
        foreach (var (predicate, reason) in rules)
        {
            if (!await predicate(context))
            {
                return reason;
            }
        }

        return null;
    }
}
