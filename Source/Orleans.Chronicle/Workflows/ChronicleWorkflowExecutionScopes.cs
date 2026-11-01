// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Tenancy;
using Cratis.Orleans.Workflows;
using Cratis.Orleans.Workflows.Scopes;
using Microsoft.Extensions.Options;

namespace Cratis.Orleans.Chronicle.Workflows;

/// <summary>
/// Represents an <see cref="IWorkflowExecutionScopes"/> that maps a scope onto a Chronicle event store namespace.
/// </summary>
/// <remarks>
/// The wave jobs of a scope live in the jobs manager the Chronicle convention names: the event store as the jobs scope and
/// the namespace of the scope as its namespace. The <see cref="ITenancy"/> of the silo, when it has one, is what establishes the tenant.
/// </remarks>
/// <param name="options">The <see cref="EventSourcedWorkflowsOptions"/>.</param>
/// <param name="tenancy">The <see cref="ITenancy"/> of the silo, if it has one.</param>
public class ChronicleWorkflowExecutionScopes(IOptions<EventSourcedWorkflowsOptions> options, ITenancy? tenancy = default) : IWorkflowExecutionScopes
{
    /// <inheritdoc/>
    public IDisposable Enter(WorkflowScope scope) => tenancy?.Establish(scope.Value) ?? NoContext.Instance;

    /// <inheritdoc/>
    public JobsManagerKey JobsManagerFor(WorkflowScope scope) => new(options.Value.EventStore.Value, options.Value.NamespaceFor(scope).Value);

    /// <inheritdoc/>
    public WorkflowScope ScopeOf(JobsManagerKey key) =>
        tenancy?.TenantFor(key.Scope, key.Namespace) is { } tenant ? new(tenant) : options.Value.ScopeFor(new EventStoreNamespaceName(key.Namespace));

    sealed class NoContext : IDisposable
    {
        public static readonly NoContext Instance = new();

        public void Dispose()
        {
        }
    }
}
