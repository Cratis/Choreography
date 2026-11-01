// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Orleans.Workflows.Graphs;

/// <summary>
/// The version of a capability.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record CapabilityVersion(int Value) : ConceptAs<int>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="int"/> to <see cref="CapabilityVersion"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator CapabilityVersion(int value) => new(value);
}
