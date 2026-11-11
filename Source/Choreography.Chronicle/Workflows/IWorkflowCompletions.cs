// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Waits;

namespace Cratis.Choreography.Chronicle.Workflows;

/// <summary>
/// Defines how the answer of a performer that suspended its node reaches the run that waits for it.
/// </summary>
public interface IWorkflowCompletions
{
    /// <summary>
    /// Completes the open completion wait that suspended under a key.
    /// </summary>
    /// <param name="scope">The <see cref="WorkflowScope"/> whose runs are searched.</param>
    /// <param name="key">The <see cref="WorkflowCompletionKey"/> the performer suspended under.</param>
    /// <param name="outlet">The <see cref="OutletName"/> the node leaves through - one it declares.</param>
    /// <param name="payload">The answer, as the node's output.</param>
    /// <param name="actor">The <see cref="WorkflowActor"/> that answered.</param>
    /// <returns>The number of waits that were completed - zero when no open wait is suspended under the key.</returns>
    /// <remarks>
    /// The waits are found in the <see cref="Listing.OpenWorkflowWait"/> read model, which can lag the run: an answer that arrives
    /// before the suspension is visible there completes nothing and is not remembered, so the caller retries until it completes
    /// one or the invocation is over. A caller that holds the run and the execution of the invocation skips the lookup and calls
    /// <see cref="Cratis.Choreography.Workflows.Runs.IWorkflowRun.ResolveWait"/> with
    /// <see cref="Cratis.Choreography.Workflows.Runs.Coordination.WaitHandling.WaitIdFor"/>. A wait that was completed or has expired is not completed again.
    /// </remarks>
    Task<int> Complete(WorkflowScope scope, WorkflowCompletionKey key, OutletName outlet, WorkflowData payload, WorkflowActor actor);
}
