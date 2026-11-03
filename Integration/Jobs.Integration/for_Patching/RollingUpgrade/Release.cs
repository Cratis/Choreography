// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Patching;

namespace Cratis.Choreography.Jobs.Integration.for_Patching.RollingUpgrade;

/// <summary>
/// A release of the host: its version and the patches it ships.
/// </summary>
/// <param name="Version">The version of the host.</param>
/// <param name="Patches">The patches the release ships, by name and version.</param>
/// <param name="PatchesOnStart">Whether a silo of this release asks for the patches to be applied when it starts.</param>
public record Release(SemanticVersion Version, IReadOnlyDictionary<string, SemanticVersion> Patches, bool PatchesOnStart = true);
