// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Choreography.Jobs.Integration.for_Patching.RollingUpgrade;

/// <summary>
/// What each silo of a rolling upgrade is: the release it runs and the patches that release ships, plus a record of which silo
/// applied which patch.
/// </summary>
/// <param name="database">The database the shared patch storage lives in.</param>
/// <param name="releases">The release each silo runs, by silo name (Primary, Secondary_1 and so on).</param>
public sealed class RollingUpgradeSignals(string database, IReadOnlyDictionary<string, Release> releases)
{
    readonly ConcurrentQueue<(string Silo, string Patch)> _applied = new();

    /// <summary>
    /// Gets the name of the database the patch storage lives in.
    /// </summary>
    public string Database { get; } = database;

    /// <summary>
    /// Gets what was applied, in order, with the silo that applied it.
    /// </summary>
    public IReadOnlyList<(string Silo, string Patch)> Applied => [.. _applied];

    /// <summary>
    /// Gets or sets how long applying a patch takes.
    /// </summary>
    public TimeSpan PatchDuration { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets how long a grain call waits for its response before it times out.
    /// </summary>
    public TimeSpan ResponseTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the pod being started right now, as the label it is recorded under and the release it runs.
    /// </summary>
    /// <remarks>
    /// Silo names are reused by the test host once a silo has been killed, so a pod that joins is identified by what the spec says it is
    /// rather than by name.
    /// </remarks>
    public (string Label, Release Release)? Joining { get; set; }

    /// <summary>
    /// Gets the release a silo runs.
    /// </summary>
    /// <param name="silo">The name of the silo.</param>
    /// <returns>The <see cref="Release"/>.</returns>
    public Release ReleaseOf(string silo) => releases[silo];

    /// <summary>
    /// Records that a silo applied a patch.
    /// </summary>
    /// <param name="silo">The name of the silo.</param>
    /// <param name="patch">The name of the patch.</param>
    public void Record(string silo, string patch) => _applied.Enqueue((silo, patch));
}
