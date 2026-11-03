// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// A reference to one version of a capability.
/// </summary>
/// <param name="Id">The <see cref="CapabilityId"/>.</param>
/// <param name="Version">The <see cref="CapabilityVersion"/>.</param>
public record CapabilityReference(CapabilityId Id, CapabilityVersion Version)
{
    /// <inheritdoc/>
    public override string ToString() => $"{Id.Value}@{Version.Value}";
}
