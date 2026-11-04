// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Choreography.Operations;

/// <summary>
/// Serializes undo, redo and execute requests for one actor in one scope within this process.
/// </summary>
public class OperationLocks
{
    readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    /// <summary>
    /// Acquire the lock for an actor in a scope.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="actor">The actor.</param>
    /// <returns>A disposable that releases the lock.</returns>
    public async Task<IDisposable> Acquire(OperationScope scope, OperationActor actor)
    {
        var semaphore = _locks.GetOrAdd($"{scope.Value.Length}:{scope.Value}{actor.Value}", _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();
        return new Releaser(semaphore);
    }

    sealed class Releaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
