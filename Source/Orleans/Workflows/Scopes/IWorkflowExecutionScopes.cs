// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;

namespace Cratis.Orleans.Workflows.Scopes;

/// <summary>
/// Defines how the engine maps an opaque <see cref="WorkflowScope"/> onto what the hosting application knows.
/// </summary>
/// <remarks>
/// The engine never interprets a scope. A host that has tenants says here which tenant a scope is, what to
/// establish while work for the scope is done, and which job system keys its wave jobs live under. The default,
/// <see cref="DefaultWorkflowExecutionScopes"/>, uses the <see cref="Tenancy.ITenancy"/> registered with the silo, when there
/// is one.
/// </remarks>
public interface IWorkflowExecutionScopes
{
    /// <summary>
    /// Establishes the context work for a scope runs in - the tenant, for a host that has them.
    /// </summary>
    /// <param name="scope">The <see cref="WorkflowScope"/>.</param>
    /// <returns>A disposable that ends the context.</returns>
    IDisposable Enter(WorkflowScope scope);

    /// <summary>
    /// Gets the key of the jobs manager that holds the wave jobs of a scope.
    /// </summary>
    /// <param name="scope">The <see cref="WorkflowScope"/>.</param>
    /// <returns>The <see cref="JobsManagerKey"/>.</returns>
    JobsManagerKey JobsManagerFor(WorkflowScope scope);

    /// <summary>
    /// Gets the scope a jobs manager key was made for - the reverse of <see cref="JobsManagerFor"/>.
    /// </summary>
    /// <param name="key">The <see cref="JobsManagerKey"/>.</param>
    /// <returns>The <see cref="WorkflowScope"/>.</returns>
    WorkflowScope ScopeOf(JobsManagerKey key);
}
