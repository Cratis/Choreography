// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Definitions;

/// <summary>
/// The provider version a capability was pinned to when a run was admitted.
/// </summary>
/// <param name="Capability">The <see cref="CapabilityReference"/>.</param>
/// <param name="Provider">The <see cref="ProviderVersion"/> behind it.</param>
public record CapabilityPin(CapabilityReference Capability, ProviderVersion Provider);
