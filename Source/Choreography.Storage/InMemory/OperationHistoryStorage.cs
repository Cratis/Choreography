// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Operations;

namespace Cratis.Choreography.Storage.InMemory;

/// <summary>
/// Represents an in-memory <see cref="IOperationHistoryStorage"/>.
/// </summary>
public class OperationHistoryStorage : IOperationHistoryStorage
{
    readonly object _lock = new();
    readonly List<OperationEntry> _entries = [];
    ulong _position;

    /// <inheritdoc/>
    public Task<OperationEntry> Append(OperationEntry entry)
    {
        lock (_lock)
        {
            var stored = entry with { Position = ++_position };
            _entries.Add(stored);
            return Task.FromResult(stored);
        }
    }

    /// <inheritdoc/>
    public Task<OperationEntry?> Update(OperationId id, Func<OperationEntry, OperationEntry> change)
    {
        lock (_lock)
        {
            var index = _entries.FindIndex(_ => _.Id == id);
            if (index < 0)
            {
                return Task.FromResult<OperationEntry?>(null);
            }

            var current = _entries[index];
            _entries[index] = change(current) with
            {
                Id = current.Id,
                Position = current.Position,
                Scope = current.Scope,
                Actor = current.Actor,
                Kind = current.Kind
            };
            return Task.FromResult<OperationEntry?>(_entries[index]);
        }
    }

    /// <inheritdoc/>
    public Task<OperationEntry?> Get(OperationId id)
    {
        lock (_lock)
        {
            return Task.FromResult(_entries.Find(_ => _.Id == id));
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<OperationEntry>> GetFor(OperationScope scope, OperationActor? actor = null)
    {
        lock (_lock)
        {
            IReadOnlyList<OperationEntry> result = [.. _entries.Where(_ => _.Scope == scope && (actor is null || _.Actor == actor))];
            return Task.FromResult(result);
        }
    }
}
