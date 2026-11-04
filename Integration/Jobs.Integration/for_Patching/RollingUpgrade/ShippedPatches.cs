// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_Patching.RollingUpgrade;

/// <summary>
/// The patches one release ships, standing in for what discovery finds in that release's assembly.
/// </summary>
/// <param name="patches">The patches.</param>
public sealed class ShippedPatches(IEnumerable<ICanApplyPatch> patches) : IInstancesOf<ICanApplyPatch>
{
    readonly List<ICanApplyPatch> _patches = [.. patches];

    /// <inheritdoc/>
    public IEnumerator<ICanApplyPatch> GetEnumerator() => _patches.GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
