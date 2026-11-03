// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Defines the storage of the operation history. Entries are appended and never deleted.
/// </summary>
public interface IOperationHistoryStorage
{
    /// <summary>
    /// Append a new entry to the history.
    /// </summary>
    /// <param name="entry">The entry to append. Its <see cref="OperationEntry.Position"/> is ignored.</param>
    /// <returns>The stored entry, with its position assigned.</returns>
    Task<OperationEntry> Append(OperationEntry entry);

    /// <summary>
    /// Change the lifecycle fields of an existing entry. Its identity, position, scope, actor and kind never change.
    /// </summary>
    /// <param name="id">The operation to change.</param>
    /// <param name="change">The change to apply to the current entry.</param>
    /// <returns>The changed entry, or null if it does not exist.</returns>
    Task<OperationEntry?> Update(OperationId id, Func<OperationEntry, OperationEntry> change);

    /// <summary>
    /// Get an entry.
    /// </summary>
    /// <param name="id">The operation to get.</param>
    /// <returns>The entry, or null if it does not exist.</returns>
    Task<OperationEntry?> Get(OperationId id);

    /// <summary>
    /// Get the entries of a scope, optionally only those of one actor, in history order.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="actor">Optional actor to narrow to.</param>
    /// <returns>The entries, oldest first.</returns>
    Task<IReadOnlyList<OperationEntry>> GetFor(OperationScope scope, OperationActor? actor = null);
}
