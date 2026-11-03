// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_Patching.RollingUpgrade;

/// <summary>
/// A patch that records which silo applied it, after taking as long as the cluster says patches take.
/// </summary>
/// <param name="name">The name of the patch.</param>
/// <param name="version">The version the patch belongs to.</param>
/// <param name="silo">The name of the silo applying it.</param>
/// <param name="signals">The <see cref="RollingUpgradeSignals"/> to record in.</param>
public sealed class RecordedPatch(string name, SemanticVersion version, string silo, RollingUpgradeSignals signals) : ICanApplyPatch
{
    /// <inheritdoc/>
    public string Name => name;

    /// <inheritdoc/>
    public SemanticVersion Version => version;

    /// <inheritdoc/>
    public async Task Up()
    {
        await Task.Delay(signals.PatchDuration);
        signals.Record(silo, name);
    }
}
