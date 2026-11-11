// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Graphs;

namespace Cratis.Choreography.Workflows.Execution;

/// <summary>
/// Defines the per-capability concurrency gates of a silo - so one slow external system cannot take every node slot it has.
/// </summary>
/// <remarks>
/// Limits come from <see cref="WorkflowsOptions.CapabilityLimits"/>. A gate bounds how many performer calls of the capabilities it
/// covers are in flight on this silo at once; it is not a rate limit and it does not count calls on other silos.
/// </remarks>
public interface IWorkflowCapabilityGates
{
    /// <summary>
    /// Waits for room in the gate of a capability.
    /// </summary>
    /// <param name="capability">The <see cref="CapabilityReference"/> about to be performed.</param>
    /// <param name="cancellationToken">Cancelled when the attempt runs out of time or the job is stopped.</param>
    /// <returns>The lease - disposing it gives the room back. Immediate when no limit covers the capability.</returns>
    ValueTask<IDisposable> Enter(CapabilityReference capability, CancellationToken cancellationToken);
}
