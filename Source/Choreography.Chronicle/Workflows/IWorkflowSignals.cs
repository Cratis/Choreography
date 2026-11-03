// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;

namespace Cratis.Choreography.Chronicle.Workflows;

/// <summary>
/// Defines how a signal reaches the runs that wait for it.
/// </summary>
public interface IWorkflowSignals
{
    /// <summary>
    /// Resolves every open signal wait whose correlation and contract match.
    /// </summary>
    /// <param name="scope">The <see cref="WorkflowScope"/> whose runs are signalled.</param>
    /// <param name="correlation">The <see cref="WorkflowCorrelationKey"/> the signal is about.</param>
    /// <param name="contract">The <see cref="WorkflowContractReference"/> of the signal.</param>
    /// <param name="payload">What came with the signal.</param>
    /// <param name="actor">The <see cref="WorkflowActor"/> that raised it.</param>
    /// <returns>The number of waits that were resolved.</returns>
    /// <remarks>
    /// The waits are found in the <see cref="Listing.OpenWorkflowWait"/> read model, which can lag the run: a signal raised
    /// before the wait it is meant for is visible there resolves nothing, and is not remembered.
    /// </remarks>
    Task<int> Signal(WorkflowScope scope, WorkflowCorrelationKey correlation, WorkflowContractReference contract, WorkflowData payload, WorkflowActor actor);
}
