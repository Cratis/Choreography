// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the default <see cref="IOperationHistory"/> on top of an <see cref="IOperationHistoryStorage"/>.
/// </summary>
/// <param name="storage">The <see cref="IOperationHistoryStorage"/>.</param>
/// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
public class OperationHistory(IOperationHistoryStorage storage, TimeProvider timeProvider) : IOperationHistory
{
    /// <inheritdoc/>
    public async Task<OperationEntry> Record(OperationScope scope, OperationActor actor, OperationKind kind, Func<IOperationRecording, Task> work)
    {
        var entry = await Begin(scope, actor, kind);
        try
        {
            await work(new Recording(this, entry.Id));
        }
        catch (Exception ex)
        {
            await Fail(entry.Id, ex.Message);
            throw;
        }

        return await Complete(entry.Id);
    }

    /// <inheritdoc/>
    public Task<OperationEntry> Begin(
        OperationScope scope,
        OperationActor actor,
        OperationKind kind,
        OperationRole role = OperationRole.Forward,
        OperationId? target = null,
        string? intent = null) =>
        storage.Append(new OperationEntry(
            OperationId.New(),
            0,
            scope,
            actor,
            kind,
            role,
            OperationStatus.Started,
            [],
            timeProvider.GetUtcNow(),
            target,
            intent,
            null,
            null));

    /// <inheritdoc/>
    public Task<OperationEntry> AssociateEvents(OperationId id, IEnumerable<ulong> eventSequenceNumbers) =>
        ChangeStarted(id, entry => entry with { EventSequenceNumbers = [.. entry.EventSequenceNumbers, .. eventSequenceNumbers] });

    /// <inheritdoc/>
    public Task<OperationEntry> Complete(OperationId id, string? memento = null) =>
        ChangeStarted(id, entry => entry with { Status = OperationStatus.Completed, Memento = memento ?? entry.Memento });

    /// <inheritdoc/>
    public Task<OperationEntry> Fail(OperationId id, string reason) =>
        ChangeStarted(id, entry => entry with { Status = OperationStatus.Failed, Failure = reason });

    /// <inheritdoc/>
    public async Task<OperationEntry> Transition(OperationId id, OperationStatus status) =>
        await storage.Update(id, entry => entry with { Status = status }) ?? throw new OperationNotFound(id);

    /// <inheritdoc/>
    public Task<OperationEntry?> Get(OperationId id) => storage.Get(id);

    /// <inheritdoc/>
    public Task<IReadOnlyList<OperationEntry>> GetFor(OperationScope scope, OperationActor? actor = null) => storage.GetFor(scope, actor);

    /// <inheritdoc/>
    public async Task<OperationEntry?> GetLatest(OperationScope scope, OperationActor actor)
    {
        var entries = await storage.GetFor(scope, actor);
        return entries.Count == 0 ? null : entries[^1];
    }

    async Task<OperationEntry> ChangeStarted(OperationId id, Func<OperationEntry, OperationEntry> change)
    {
        OperationStatus? rejected = null;
        var updated = await storage.Update(id, entry =>
        {
            if (entry.Status != OperationStatus.Started)
            {
                rejected = entry.Status;
                return entry;
            }

            return change(entry);
        }) ?? throw new OperationNotFound(id);

        if (rejected is not null)
        {
            throw new OperationNotStarted(id, rejected.Value);
        }

        return updated;
    }

    sealed class Recording(OperationHistory history, OperationId id) : IOperationRecording
    {
        public OperationId Id => id;

        public Task Associate(IEnumerable<ulong> eventSequenceNumbers) => history.AssociateEvents(id, eventSequenceNumbers);
    }
}
