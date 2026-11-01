// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Workflows.Scopes;

namespace Cratis.Orleans.Workflows;

/// <summary>
/// The opaque scope - typically a tenant or a namespace - that workflows and runs belong to.
/// </summary>
/// <param name="Value">The underlying value.</param>
/// <remarks>
/// The engine never interprets a scope. It is part of every grain key, so a call can never move a run to
/// another scope, and it is handed to <see cref="IWorkflowExecutionScopes"/> to establish whatever the host needs
/// around work done for the scope - a tenant, a namespace, an event store.
/// </remarks>
public record WorkflowScope(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The scope of a host that has only one.
    /// </summary>
    public static readonly WorkflowScope Default = new("default");

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowScope"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowScope(string value) => new(value);
}
