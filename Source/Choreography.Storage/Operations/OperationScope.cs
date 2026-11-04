// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Choreography.Operations;

/// <summary>
/// Represents the scope an operation belongs to - for example a document, a model or a tenant. It is opaque to
/// Choreography; every consumer maps its own identity onto it.
/// </summary>
/// <param name="Value">The actual value.</param>
public record OperationScope(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly convert from <see cref="string"/> to <see cref="OperationScope"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert from.</param>
    public static implicit operator OperationScope(string value) => new(value);
}
