// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// A node's configuration, as a JSON document.
/// </summary>
/// <param name="Json">The JSON text.</param>
/// <remarks>
/// Configuration is text rather than a typed payload so that the shape of an adapter's settings never
/// becomes the shape of an event: a run records its graph, and events live forever. It must never
/// contain a secret - credentials are referred to by key and resolved by the adapter.
/// </remarks>
public record WorkflowNodeConfiguration(string Json) : ConceptAs<string>(Json)
{
    /// <summary>
    /// The empty configuration.
    /// </summary>
    public static readonly WorkflowNodeConfiguration Empty = new("{}");

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="WorkflowNodeConfiguration"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator WorkflowNodeConfiguration(string value) => new(value);
}
