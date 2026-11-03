// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the kind of an operation, for example the name of the command it performs.
/// </summary>
/// <param name="Value">The actual value.</param>
public record OperationKind(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="OperationKind"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert from.</param>
    public static implicit operator OperationKind(string value) => new(value);
}
