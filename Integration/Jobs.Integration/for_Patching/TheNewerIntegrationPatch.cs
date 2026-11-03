// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_Patching;

/// <summary>
/// A second patch, with a newer version than <see cref="TheOlderIntegrationPatch"/>.
/// </summary>
/// <param name="probe">The <see cref="PatchProbe"/> the silo registered.</param>
public class TheNewerIntegrationPatch(PatchProbe probe) : ICanApplyPatch
{
    /// <inheritdoc/>
    public SemanticVersion Version => new(1, 6, 0);

    /// <inheritdoc/>
    public Task Up()
    {
        probe.Record(((ICanApplyPatch)this).Name);
        return Task.CompletedTask;
    }
}
