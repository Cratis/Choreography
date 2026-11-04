// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the default <see cref="IOperationEngine"/>.
/// </summary>
/// <param name="history">The <see cref="IOperationHistory"/>.</param>
/// <param name="definitions">The <see cref="IOperationDefinitions"/>.</param>
/// <param name="executor">The host's <see cref="IOperationExecutor"/>.</param>
/// <param name="locks">The <see cref="OperationLocks"/>.</param>
/// <param name="services">The host's <see cref="IServiceProvider"/>, handed to undo and redo rules.</param>
public class OperationEngine(
    IOperationHistory history,
    IOperationDefinitions definitions,
    IOperationExecutor executor,
    OperationLocks locks,
    IServiceProvider services) : IOperationEngine
{
    /// <inheritdoc/>
    public async Task<OperationOutcome> Execute<TCommand>(OperationScope scope, OperationActor actor, TCommand command)
        where TCommand : notnull
    {
        var definition = definitions.Find(typeof(TCommand));
        if (definition is null)
        {
            return OperationOutcome.Blocked($"No operation is defined for {typeof(TCommand).Name}");
        }

        using var guard = await locks.Acquire(scope, actor);
        var result = await Run(scope, actor, definition.Kind, OperationRole.Forward, null, command, definition.SerializeIntent(command));
        if (result.Outcome.Kind != OperationOutcomeKind.Succeeded)
        {
            return result.Outcome;
        }

        await DiscardRedoStack(scope, actor);
        return result.Outcome;
    }

    /// <inheritdoc/>
    public async Task<OperationOutcome> Undo(OperationScope scope, OperationActor actor)
    {
        using var guard = await locks.Acquire(scope, actor);
        var entries = await history.GetFor(scope, actor);
        var target = FindUndoTarget(entries);
        if (target is null)
        {
            return OperationOutcome.NothingToUndo;
        }

        var (definition, reason) = await EvaluateUndo(target);
        if (reason is not null)
        {
            return OperationOutcome.Blocked(reason);
        }

        var inverse = await definition!.CreateUndo(target, services);
        var result = await Run(scope, actor, target.Kind, OperationRole.Undo, target.Id, inverse, JsonSerializer.Serialize(inverse, inverse.GetType(), OperationJson.Options));
        if (result.Outcome.Kind == OperationOutcomeKind.Succeeded)
        {
            await history.Transition(target.Id, OperationStatus.Undone);
        }

        return result.Outcome;
    }

    /// <inheritdoc/>
    public async Task<OperationOutcome> Redo(OperationScope scope, OperationActor actor)
    {
        using var guard = await locks.Acquire(scope, actor);
        var entries = await history.GetFor(scope, actor);
        var target = FindRedoTarget(entries);
        if (target is null)
        {
            return OperationOutcome.NothingToRedo;
        }

        var (definition, reason) = await EvaluateRedo(target);
        if (reason is not null)
        {
            return OperationOutcome.Blocked(reason);
        }

        var forward = await definition!.CreateRedo(target, services);
        var result = await Run(scope, actor, target.Kind, OperationRole.Redo, target.Id, forward, definition.SerializeIntent(forward));
        if (result.Outcome.Kind == OperationOutcomeKind.Succeeded)
        {
            await history.Transition(target.Id, OperationStatus.Redone);
        }

        return result.Outcome;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<OperationHistoryItem>> GetHistory(OperationScope scope, OperationActor actor)
    {
        var entries = await history.GetFor(scope, actor);
        var undoTarget = FindUndoTarget(entries);
        var redoTarget = FindRedoTarget(entries);
        var undoReason = undoTarget is null ? null : (await EvaluateUndo(undoTarget)).Reason;
        var redoReason = redoTarget is null ? null : (await EvaluateRedo(redoTarget)).Reason;

        return [.. entries.Select(entry =>
        {
            var isUndoTarget = entry.Id == undoTarget?.Id;
            var isRedoTarget = entry.Id == redoTarget?.Id;
            var blocked = isUndoTarget ? undoReason : null;
            blocked ??= isRedoTarget ? redoReason : null;
            return new OperationHistoryItem(
                entry.Id.Value,
                entry.Position,
                entry.Scope.Value,
                entry.Actor.Value,
                entry.Kind.Value,
                entry.Role,
                entry.Status,
                entry.EventSequenceNumbers,
                entry.Started,
                entry.Target?.Value,
                isUndoTarget && blocked is null,
                isRedoTarget && blocked is null,
                blocked);
        })];
    }

    static OperationEntry? FindUndoTarget(IReadOnlyList<OperationEntry> entries)
    {
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            if (entries[i].Role != OperationRole.Undo && entries[i].Status == OperationStatus.Completed)
            {
                return entries[i];
            }
        }

        return null;
    }

    static OperationEntry? FindRedoTarget(IReadOnlyList<OperationEntry> entries)
    {
        for (var i = entries.Count - 1; i >= 0; i--)
        {
            var candidate = entries[i];
            if (candidate.Role == OperationRole.Undo && candidate.Status == OperationStatus.Completed)
            {
                var undone = entries.FirstOrDefault(_ => _.Id == candidate.Target && _.Status == OperationStatus.Undone);
                if (undone is not null)
                {
                    return undone;
                }
            }
        }

        return null;
    }

    async Task<(IOperationDefinition? Definition, string? Reason)> EvaluateUndo(OperationEntry target)
    {
        var definition = definitions.Find(target.Kind);
        if (definition is null)
        {
            return (null, $"No operation definition exists for '{target.Kind.Value}'");
        }

        if (!definition.CanBeUndone)
        {
            return (definition, $"'{target.Kind.Value}' does not define how to undo it");
        }

        return (definition, await definition.EvaluateUndo(target, services));
    }

    async Task<(IOperationDefinition? Definition, string? Reason)> EvaluateRedo(OperationEntry target)
    {
        var definition = definitions.Find(target.Kind);
        if (definition is null)
        {
            return (null, $"No operation definition exists for '{target.Kind.Value}'");
        }

        return (definition, await definition.EvaluateRedo(target, services));
    }

    async Task DiscardRedoStack(OperationScope scope, OperationActor actor)
    {
        var entries = await history.GetFor(scope, actor);
        foreach (var undone in entries.Where(_ => _.Status == OperationStatus.Undone))
        {
            await history.Transition(undone.Id, OperationStatus.Discarded);
        }
    }

    async Task<(OperationOutcome Outcome, OperationId Id)> Run(
        OperationScope scope,
        OperationActor actor,
        OperationKind kind,
        OperationRole role,
        OperationId? target,
        object command,
        string intent)
    {
        var entry = await history.Begin(scope, actor, kind, role, target, intent);
        OperationExecutionResult result;
        try
        {
            result = await executor.Execute(command, new OperationExecutionContext(entry.Id, scope, actor, kind, role, target));
        }
        catch (Exception ex)
        {
            result = OperationExecutionResult.Failure(ex.Message);
        }

        if (result.EventSequenceNumbers.Count > 0)
        {
            await history.AssociateEvents(entry.Id, result.EventSequenceNumbers);
        }

        if (!result.Succeeded)
        {
            var reason = result.Error ?? "The command failed";
            await history.Fail(entry.Id, reason);
            return (OperationOutcome.Failed(entry.Id, reason), entry.Id);
        }

        var memento = result.Memento is null ? null : JsonSerializer.Serialize(result.Memento, result.Memento.GetType(), OperationJson.Options);
        await history.Complete(entry.Id, memento);
        return (OperationOutcome.Succeeded(entry.Id), entry.Id);
    }
}
