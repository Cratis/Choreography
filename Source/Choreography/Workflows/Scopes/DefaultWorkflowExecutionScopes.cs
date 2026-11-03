// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Tenancy;
using Microsoft.Extensions.Options;

namespace Cratis.Choreography.Workflows.Scopes;

/// <summary>
/// Represents the default <see cref="IWorkflowExecutionScopes"/>.
/// </summary>
/// <remarks>
/// A scope is the tenant when the silo has an <see cref="ITenancy"/>, and nothing is established when it has not. The
/// wave jobs of a scope live under the jobs scope <see cref="WorkflowsOptions.JobsScope"/> and a namespace that is the
/// scope itself.
/// </remarks>
/// <param name="options">The <see cref="WorkflowsOptions"/>.</param>
/// <param name="tenancy">The <see cref="ITenancy"/> of the silo, if it has one.</param>
public class DefaultWorkflowExecutionScopes(IOptions<WorkflowsOptions> options, ITenancy? tenancy = default) : IWorkflowExecutionScopes
{
    /// <inheritdoc/>
    public IDisposable Enter(WorkflowScope scope) => tenancy?.Establish(scope.Value) ?? NoContext.Instance;

    /// <inheritdoc/>
    public JobsManagerKey JobsManagerFor(WorkflowScope scope) => new(options.Value.JobsScope, scope.Value);

    /// <inheritdoc/>
    public WorkflowScope ScopeOf(JobsManagerKey key) => new(tenancy?.TenantFor(key.Scope, key.Namespace) ?? key.Namespace);

    sealed class NoContext : IDisposable
    {
        public static readonly NoContext Instance = new();

        public void Dispose()
        {
        }
    }
}
