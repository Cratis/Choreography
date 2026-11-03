// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Workflows.Definitions;

/// <summary>
/// The version of the provider behind a capability at the time a run was admitted.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ProviderVersion(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="ProviderVersion"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    public static implicit operator ProviderVersion(string value) => new(value);
}
