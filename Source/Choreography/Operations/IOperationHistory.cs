// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Defines the operation history: records logical operations, the events they produced, and answers queries
/// about them. The history is append-only; the events themselves are never touched.
/// </summary>
public interface IOperationHistory
{
    /// <summary>
    /// Record a logical operation around a unit of work. The operation is completed when the work returns and
    /// failed - then the exception is rethrown - when it throws.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="actor">The actor.</param>
    /// <param name="kind">The kind of operation.</param>
    /// <param name="work">The work, associating the events it produces through the <see cref="IOperationRecording"/>.</param>
    /// <returns>The completed entry.</returns>
    Task<OperationEntry> Record(OperationScope scope, OperationActor actor, OperationKind kind, Func<IOperationRecording, Task> work);

    /// <summary>
    /// Start recording an operation by hand. Finish it with <see cref="Complete"/> or <see cref="Fail"/>.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="actor">The actor.</param>
    /// <param name="kind">The kind of operation.</param>
    /// <param name="role">The role in the actor's history.</param>
    /// <param name="target">The operation an undo or redo applies to.</param>
    /// <param name="intent">Serialized intent of the operation.</param>
    /// <returns>The started entry.</returns>
    Task<OperationEntry> Begin(
        OperationScope scope,
        OperationActor actor,
        OperationKind kind,
        OperationRole role = OperationRole.Forward,
        OperationId? target = null,
        string? intent = null);

    /// <summary>
    /// Associate event sequence numbers with a started operation.
    /// </summary>
    /// <param name="id">The operation.</param>
    /// <param name="eventSequenceNumbers">The sequence numbers of the events produced.</param>
    /// <returns>The changed entry.</returns>
    Task<OperationEntry> AssociateEvents(OperationId id, IEnumerable<ulong> eventSequenceNumbers);

    /// <summary>
    /// Complete a started operation.
    /// </summary>
    /// <param name="id">The operation.</param>
    /// <param name="memento">Serialized state captured while executing, needed to reverse the operation later.</param>
    /// <returns>The changed entry.</returns>
    Task<OperationEntry> Complete(OperationId id, string? memento = null);

    /// <summary>
    /// Fail a started operation.
    /// </summary>
    /// <param name="id">The operation.</param>
    /// <param name="reason">Why it failed.</param>
    /// <returns>The changed entry.</returns>
    Task<OperationEntry> Fail(OperationId id, string reason);

    /// <summary>
    /// Move a finished operation to another lifecycle status, for example <see cref="OperationStatus.Undone"/>.
    /// </summary>
    /// <param name="id">The operation.</param>
    /// <param name="status">The new status.</param>
    /// <returns>The changed entry.</returns>
    Task<OperationEntry> Transition(OperationId id, OperationStatus status);

    /// <summary>
    /// Get an operation.
    /// </summary>
    /// <param name="id">The operation.</param>
    /// <returns>The entry, or null if it does not exist.</returns>
    Task<OperationEntry?> Get(OperationId id);

    /// <summary>
    /// Get the operations of a scope, optionally of one actor, in order.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="actor">Optional actor to narrow to.</param>
    /// <returns>The entries, oldest first.</returns>
    Task<IReadOnlyList<OperationEntry>> GetFor(OperationScope scope, OperationActor? actor = null);

    /// <summary>
    /// Get the latest operation, of any role, an actor performed in a scope.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="actor">The actor.</param>
    /// <returns>The entry, or null if the actor has no operations in the scope.</returns>
    Task<OperationEntry?> GetLatest(OperationScope scope, OperationActor actor);
}
