// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Sweeping;

/// <summary>
/// Defines a source of the scopes whose runs the engine keeps going.
/// </summary>
/// <remarks>
/// The sweep and the recovery of interrupted wave jobs act on the scopes they know. A silo knows the scopes it has served
/// since it started and those in <see cref="WorkflowsOptions.Scopes"/>; a host that has more - every tenant, say -
/// implements this and registers it, so a run waiting in a scope no call has touched since a full restart is still found.
/// </remarks>
public interface IWorkflowScopeSource
{
    /// <summary>
    /// Gets the scopes.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The scopes.</returns>
    Task<IEnumerable<WorkflowScope>> GetScopes(CancellationToken cancellationToken);
}
