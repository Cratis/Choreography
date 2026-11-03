// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Choreography.Patching;
using Cratis.Choreography.Storage.Patching;

namespace Cratis.Choreography.Storage;

/// <summary>
/// Represents an in-memory implementation of <see cref="IPatchStorage"/>.
/// </summary>
/// <remarks>
/// Nothing survives the process, so every start applies every patch again. Use it for development and tests.
/// </remarks>
public sealed class InMemoryPatchStorage : IPatchStorage
{
    readonly ConcurrentDictionary<string, AppliedPatch> _patches = new();
    SemanticVersion? _patchedVersion;
    SemanticVersion? _hostVersion;

    /// <inheritdoc/>
    public Task<IEnumerable<AppliedPatch>> GetAll() => Task.FromResult<IEnumerable<AppliedPatch>>([.. _patches.Values]);

    /// <inheritdoc/>
    public Task<bool> Has(string patchName) => Task.FromResult(_patches.ContainsKey(patchName));

    /// <inheritdoc/>
    public Task Save(AppliedPatch patch)
    {
        _patches[patch.Name] = patch;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<SemanticVersion?> GetPatchedVersion() => Task.FromResult(Volatile.Read(ref _patchedVersion));

    /// <inheritdoc/>
    public Task SetPatchedVersion(SemanticVersion version)
    {
        Volatile.Write(ref _patchedVersion, version);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<SemanticVersion?> GetHostVersion() => Task.FromResult(Volatile.Read(ref _hostVersion));

    /// <inheritdoc/>
    public Task SetHostVersion(SemanticVersion version)
    {
        Volatile.Write(ref _hostVersion, version);
        return Task.CompletedTask;
    }
}
