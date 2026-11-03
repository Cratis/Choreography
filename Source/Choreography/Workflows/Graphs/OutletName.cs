// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// The name of a node's outlet - the named way out of a node an edge is connected to.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record OutletName(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// No outlet - an outcome that was not routed anywhere.
    /// </summary>
    public static readonly OutletName None = new(string.Empty);

    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="OutletName"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator OutletName(string value) => new(value);
}
