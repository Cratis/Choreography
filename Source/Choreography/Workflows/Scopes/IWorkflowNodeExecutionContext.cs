// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Scopes;

/// <summary>
/// Defines a hook around the work of one node - to run it as the system, for a host that authorizes work.
/// </summary>
/// <remarks>
/// Node work is done by a job step, outside any request. A host that needs to say who it acts as registers an
/// implementation; without one, nothing is established.
/// </remarks>
public interface IWorkflowNodeExecutionContext
{
    /// <summary>
    /// Begins the context a node's work runs in.
    /// </summary>
    /// <param name="services">The <see cref="IServiceProvider"/> of the scope the work is resolved from.</param>
    /// <returns>A disposable that ends the context.</returns>
    IDisposable Begin(IServiceProvider services);
}
