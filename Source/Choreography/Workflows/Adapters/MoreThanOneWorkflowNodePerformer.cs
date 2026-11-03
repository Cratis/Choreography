// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Adapters;

/// <summary>
/// The exception that is thrown when more than one performer claims the same capability.
/// </summary>
/// <param name="capability">The <see cref="CapabilityReference"/> claimed twice.</param>
public class MoreThanOneWorkflowNodePerformer(CapabilityReference capability)
    : Exception($"More than one workflow node performer claims '{capability}'");
