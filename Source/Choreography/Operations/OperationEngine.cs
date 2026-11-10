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
        var result = await Run(new RunRequest(scope, actor, definition.Kind, OperationRole.Forward, null, command, definition.SerializeIntent(command)));
        if (result.Outcome.Kind != OperationOutcomeKind.Succeeded)
        {
            return result.Outcome;
        }

        await DiscardRedoStack(scope, actor);
        return result.Outcome;
    }

    /// <inheritdoc/>
    public Task<OperationOutcome> Undo(OperationScope scope, OperationActor actor) => Reverse(scope, actor, OperationRole.Undo);

    /// <inheritdoc/>
    public Task<OperationOutcome> Redo(OperationScope scope, OperationActor actor) => Reverse(scope, actor, OperationRole.Redo);

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

    async Task<OperationOutcome> Reverse(OperationScope scope, OperationActor actor, OperationRole role)
    {
        var isUndo = role == OperationRole.Undo;
        using var guard = await locks.Acquire(scope, actor);
        var entries = await history.GetFor(scope, actor);
        var target = isUndo ? FindUndoTarget(entries) : FindRedoTarget(entries);
        if (target is null)
        {
            return isUndo ? OperationOutcome.NothingToUndo : OperationOutcome.NothingToRedo;
        }

        var (definition, reason) = await (isUndo ? EvaluateUndo(target) : EvaluateRedo(target));
        if (reason is not null)
        {
            return OperationOutcome.Blocked(reason);
        }

        var command = await (isUndo ? definition!.CreateUndo(target, services) : definition!.CreateRedo(target, services));
        var intent = isUndo ? JsonSerializer.Serialize(command, command.GetType(), OperationJson.Options) : definition.SerializeIntent(command);
        var result = await Run(new RunRequest(scope, actor, target.Kind, role, target.Id, command, intent));
        return await TransitionOnSuccess(result.Outcome, target, isUndo ? OperationStatus.Undone : OperationStatus.Redone);
    }

    async Task<OperationOutcome> TransitionOnSuccess(OperationOutcome outcome, OperationEntry target, OperationStatus status)
    {
        if (outcome.Kind == OperationOutcomeKind.Succeeded)
        {
            await history.Transition(target.Id, status);
        }

        return outcome;
    }

    async Task DiscardRedoStack(OperationScope scope, OperationActor actor)
    {
        var entries = await history.GetFor(scope, actor);
        foreach (var undone in entries.Where(_ => _.Status == OperationStatus.Undone))
        {
            await history.Transition(undone.Id, OperationStatus.Discarded);
        }
    }

    async Task<(OperationOutcome Outcome, OperationId Id)> Run(RunRequest request)
    {
        var (scope, actor, kind, role, target, command, intent) = request;
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

    sealed record RunRequest(
        OperationScope Scope,
        OperationActor Actor,
        OperationKind Kind,
        OperationRole Role,
        OperationId? Target,
        object Command,
        string Intent);
}
