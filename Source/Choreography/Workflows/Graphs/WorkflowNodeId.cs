// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// The identity of a node within one workflow graph.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record WorkflowNodeId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowNodeId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowNodeId(string value) => new(value);
}
