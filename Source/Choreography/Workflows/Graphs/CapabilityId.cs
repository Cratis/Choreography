// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Graphs;

/// <summary>
/// The identity of a capability - what a node does, provided by an area or the engine itself.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record CapabilityId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="CapabilityId"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator CapabilityId(string value) => new(value);
}
