// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Patching;

/// <summary>
/// Represents the silo lifecycle participant that applies patches when the silo becomes active.
/// </summary>
/// <param name="patchRunner">The <see cref="IPatchRunner"/> that applies the patches.</param>
internal sealed class PatchingStartupTask(IPatchRunner patchRunner) : ILifecycleParticipant<ISiloLifecycle>
{
    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(PatchingStartupTask), ServiceLifecycleStage.Active, patchRunner.ApplyPatches);
}
