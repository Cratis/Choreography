// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Storage.Patching;

/// <summary>
/// Defines the storage the patch manager keeps its facts in: which patches have been applied, how far patching has come,
/// and which host version last patched the store.
/// </summary>
/// <remarks>
/// There is one set of these facts per store, not per scope or namespace - a patch changes the store itself, so every silo
/// of a cluster must read and write the same ones.
/// </remarks>
public interface IPatchStorage
{
    /// <summary>
    /// Gets every patch that has been applied.
    /// </summary>
    /// <returns>The <see cref="AppliedPatch"/> records.</returns>
    Task<IEnumerable<AppliedPatch>> GetAll();

    /// <summary>
    /// Gets whether a patch has been applied.
    /// </summary>
    /// <param name="patchName">The name of the patch.</param>
    /// <returns>True if the patch has been applied, false otherwise.</returns>
    Task<bool> Has(string patchName);

    /// <summary>
    /// Records a patch as applied.
    /// </summary>
    /// <param name="patch">The <see cref="AppliedPatch"/> to record.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Recording a patch that is already recorded replaces its record.
    /// </remarks>
    Task Save(AppliedPatch patch);

    /// <summary>
    /// Gets the version of the newest patch that has been applied.
    /// </summary>
    /// <returns>The <see cref="SemanticVersion"/>, or null when no patch has been applied.</returns>
    Task<SemanticVersion?> GetPatchedVersion();

    /// <summary>
    /// Sets the version of the newest patch that has been applied.
    /// </summary>
    /// <param name="version">The <see cref="SemanticVersion"/> to record.</param>
    /// <returns>Awaitable task.</returns>
    Task SetPatchedVersion(SemanticVersion version);

    /// <summary>
    /// Gets the version of the newest host that has patched the store.
    /// </summary>
    /// <returns>The <see cref="SemanticVersion"/>, or null when no host with a known version has patched the store.</returns>
    Task<SemanticVersion?> GetHostVersion();

    /// <summary>
    /// Sets the version of the newest host that has patched the store.
    /// </summary>
    /// <param name="version">The <see cref="SemanticVersion"/> to record.</param>
    /// <returns>Awaitable task.</returns>
    Task SetHostVersion(SemanticVersion version);
}
