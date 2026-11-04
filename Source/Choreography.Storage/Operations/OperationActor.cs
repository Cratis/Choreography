// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the actor - typically a user - that performed an operation. It is opaque to Choreography.
/// </summary>
/// <param name="Value">The actual value.</param>
public record OperationActor(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="OperationActor"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert from.</param>
    public static implicit operator OperationActor(string value) => new(value);
}
