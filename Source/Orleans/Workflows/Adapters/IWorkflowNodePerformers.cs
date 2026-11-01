// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Graphs;

namespace Cratis.Orleans.Workflows.Adapters;

/// <summary>
/// Defines the registry of every <see cref="IWorkflowNodePerformer"/>.
/// </summary>
public interface IWorkflowNodePerformers
{
    /// <summary>
    /// Gets the performer of a capability.
    /// </summary>
    /// <param name="capability">The <see cref="CapabilityReference"/>.</param>
    /// <returns>The <see cref="IWorkflowNodePerformer"/>, or null when none is registered.</returns>
    IWorkflowNodePerformer? For(CapabilityReference capability);
}
